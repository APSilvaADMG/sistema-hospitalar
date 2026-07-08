import { type ReactNode, useEffect, useMemo, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { NavIcon } from './NavIcon';
import { getBranchIcon, getLeafIcon, getSectionIcon } from '../navigation/menuIcons';
import { isMenuHomePath, resolveMenuProfile } from '../navigation/menuProfile';
import { loadHospitalParams } from '../config/clinicOnDoctorProfile';
import { buildMenuSections, groupSectionsByMegaGroup } from '../navigation/sidebarMenu';
import { isMenuBranch, type MenuLeaf, type MenuMegaGroupId, type MenuNode, type MenuSection } from '../navigation/types';
import type { NavIconName } from './NavIcon';
import { useOpenModuleSearch } from './ModuleSearchProvider';

/** Seções ordenadas pela jornada do paciente (destaque visual no menu). */
const JOURNEY_FLOW_SECTIONS = new Set([
  'entrada', 'ambulatorio', 'pronto-atendimento', 'internacao', 'cirurgia',
  'pep', 'enfermagem', 'ccih', 'alta-clinica', 'fechamento', 'saida',
]);

type SidebarNavProps = {
  isStaff: boolean;
  isAdminOrReception: boolean;
  isAdmin: boolean;
  hasSecurityLgpd: boolean;
  unreadCount: number;
};

function ProfileShortcuts({ label, shortcuts }: { label: string; shortcuts: { label: string; path: string; icon: NavIconName; end?: boolean }[] }) {
  return (
    <div className="nav-profile-shortcuts no-print">
      <p className="nav-shortcuts-eyebrow">Atalhos · {label}</p>
      <div className="nav-shortcuts-grid">
        {shortcuts.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            end={item.end ?? item.path === '/'}
            className={({ isActive }) => `nav-shortcut-chip${isActive ? ' active' : ''}`}
          >
            <NavIcon name={item.icon} />
            <span>{item.label}</span>
          </NavLink>
        ))}
      </div>
    </div>
  );
}

function isPathActive(pathname: string, to: string, end?: boolean) {
  if (end || to === '/') return pathname === to;
  return pathname === to || pathname.startsWith(`${to}/`);
}

function nodeHasActive(pathname: string, node: MenuNode): boolean {
  if (isMenuBranch(node)) {
    return node.children.some((child) => nodeHasActive(pathname, child));
  }
  return isPathActive(pathname, node.path, node.end);
}

function sectionHasActive(pathname: string, section: MenuSection) {
  return section.nodes.some((node) => nodeHasActive(pathname, node));
}

function NavItemLabel({ icon, label, className }: { icon?: NavIconName; label: string; className?: string }) {
  return (
    <span className={`nav-item-row${className ? ` ${className}` : ''}`}>
      {icon ? (
        <span className="nav-link-icon" aria-hidden>
          <NavIcon name={icon} />
        </span>
      ) : null}
      <span className="nav-item-text">{label}</span>
    </span>
  );
}

function NavCollapsible({
  label,
  icon,
  depth,
  expanded,
  onToggle,
  hasActive,
  children,
  className,
  sectionId,
}: {
  label: string;
  icon?: NavIconName;
  depth: number;
  expanded: boolean;
  onToggle: () => void;
  hasActive: boolean;
  children: ReactNode;
  className?: string;
  sectionId?: string;
}) {
  const isRoot = depth === 0;
  const groupClass = isRoot ? 'nav-menu-group' : 'nav-subgroup';
  const parentClass = isRoot ? 'nav-menu-parent' : 'nav-subgroup-parent';

  return (
    <div
      className={`${groupClass}${expanded ? ' expanded' : ''}${hasActive ? ' has-active' : ''}${className ? ` ${className}` : ''}${sectionId && JOURNEY_FLOW_SECTIONS.has(sectionId) ? ' nav-journey-step' : ''}`}
      data-depth={depth}
      data-section={sectionId}
    >
      <button
        type="button"
        className={parentClass}
        onClick={onToggle}
        aria-expanded={expanded}
      >
        <NavItemLabel
          icon={icon}
          label={label}
          className={isRoot ? 'nav-menu-parent-label' : 'nav-subgroup-label'}
        />
        <span className="nav-menu-chevron" aria-hidden />
      </button>
      <div className={isRoot ? 'nav-submenu' : 'nav-subgroup-menu'}>
        <div className={isRoot ? 'nav-submenu-inner' : 'nav-subgroup-inner'}>{children}</div>
      </div>
    </div>
  );
}

function NavLeafLink({ item }: { item: MenuLeaf }) {
  const icon = getLeafIcon(item.id);
  return (
    <NavLink
      to={item.path}
      end={item.end ?? item.path === '/'}
      className={({ isActive }) => `nav-sublink${isActive ? ' active' : ''}`}
    >
      <span className="nav-item-row nav-sublink-row">
        <span className="nav-link-icon nav-sublink-icon" aria-hidden>
          <NavIcon name={icon} />
        </span>
        <span className="nav-sublink-label">{item.label}</span>
      </span>
      {item.badge ? <span className="nav-badge">{item.badge}</span> : null}
    </NavLink>
  );
}

function collectBranchDescendantIds(nodes: MenuNode[]): string[] {
  const ids: string[] = [];
  for (const node of nodes) {
    if (isMenuBranch(node)) {
      ids.push(node.id);
      ids.push(...collectBranchDescendantIds(node.children));
    }
  }
  return ids;
}

function buildBranchDescendantMap(sections: MenuSection[]): Map<string, string[]> {
  const map = new Map<string, string[]>();
  function walk(nodes: MenuNode[]) {
    for (const node of nodes) {
      if (isMenuBranch(node)) {
        map.set(node.id, collectBranchDescendantIds(node.children));
        walk(node.children);
      }
    }
  }
  for (const section of sections) walk(section.nodes);
  return map;
}

function MenuTree({
  nodes,
  depth,
  pathname,
  openIds,
  onToggle,
}: {
  nodes: MenuNode[];
  depth: number;
  pathname: string;
  openIds: Set<string>;
  onToggle: (id: string, siblingIds: string[]) => void;
}) {
  const branchSiblingIds = nodes.filter(isMenuBranch).map((node) => node.id);

  return (
    <>
      {nodes.map((node) => {
        if (isMenuBranch(node)) {
          const hasActive = nodeHasActive(pathname, node);
          return (
            <NavCollapsible
              key={node.id}
              label={node.label}
              icon={getBranchIcon(node.id)}
              depth={depth}
              expanded={openIds.has(node.id)}
              onToggle={() => onToggle(node.id, branchSiblingIds)}
              hasActive={hasActive}
            >
              <MenuTree
                nodes={node.children}
                depth={depth + 1}
                pathname={pathname}
                openIds={openIds}
                onToggle={onToggle}
              />
            </NavCollapsible>
          );
        }

        return <NavLeafLink key={node.id} item={node} />;
      })}
    </>
  );
}

function MegaGroupHeader({
  id,
  title,
  subtitle,
  color,
  accent,
  icon,
  expanded,
  onToggle,
  hasActive,
}: {
  id: MenuMegaGroupId;
  title: string;
  subtitle: string;
  color: string;
  accent: string;
  icon: NavIconName;
  expanded: boolean;
  onToggle: () => void;
  hasActive: boolean;
}) {
  return (
    <button
      type="button"
      className={`nav-mega-header${expanded ? ' expanded' : ''}${hasActive ? ' has-active' : ''}`}
      onClick={onToggle}
      aria-expanded={expanded}
      data-mega={id}
      style={{ '--mega-color': color, '--mega-accent': accent } as React.CSSProperties}
    >
      <span className="nav-mega-icon" aria-hidden>
        <NavIcon name={icon} />
      </span>
      <span className="nav-mega-stripe" aria-hidden />
      <span className="nav-mega-text">
        <span className="nav-mega-title">{title}</span>
        <span className="nav-mega-subtitle">{subtitle}</span>
      </span>
      <span className="nav-mega-chevron" aria-hidden />
    </button>
  );
}

export function SidebarNav({ isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd, unreadCount }: SidebarNavProps) {
  const { pathname } = useLocation();
  const { user, hasPermission } = useAuth();
  const openModuleSearch = useOpenModuleSearch();

  const menuProfile = useMemo(
    () => resolveMenuProfile({
      role: user?.role,
      isAdmin,
      isAdminOrReception,
      hasPermission,
    }),
    [user?.role, isAdmin, isAdminOrReception, hasPermission],
  );

  const megaGroups = useMemo(
    () => groupSectionsByMegaGroup(
      buildMenuSections({
        isStaff,
        isAdminOrReception,
        isAdmin,
        hasSecurityLgpd,
        unreadCount,
        hasPermission,
        modules: loadHospitalParams().modules,
      }),
    ),
    [isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd, unreadCount, hasPermission, pathname],
  );

  const activeSectionId = useMemo(() => {
    for (const { sections } of megaGroups) {
      const found = sections.find((section) => sectionHasActive(pathname, section));
      if (found) return found.id;
    }
    return null;
  }, [megaGroups, pathname]);

  const activeMegaId = useMemo(() => {
    for (const group of megaGroups) {
      if (group.sections.some((s) => sectionHasActive(pathname, s))) {
        return group.meta.id;
      }
    }
    return null;
  }, [megaGroups, pathname]);

  const activeBranchIds = useMemo(() => {
    const ids = new Set<string>();
    function walk(nodes: MenuNode[]) {
      for (const node of nodes) {
        if (isMenuBranch(node)) {
          if (nodeHasActive(pathname, node)) ids.add(node.id);
          walk(node.children);
        }
      }
    }
    for (const { sections } of megaGroups) {
      for (const section of sections) walk(section.nodes);
    }
    return ids;
  }, [megaGroups, pathname]);

  const branchDescendantMap = useMemo(() => {
    const map = new Map<string, string[]>();
    for (const { sections } of megaGroups) {
      for (const [id, descendants] of buildBranchDescendantMap(sections)) {
        map.set(id, descendants);
      }
    }
    return map;
  }, [megaGroups]);

  const [openMegaIds, setOpenMegaIds] = useState<Set<MenuMegaGroupId>>(() => new Set([menuProfile.homeMegaGroup]));
  const [openSectionId, setOpenSectionId] = useState<string | null>(menuProfile.homeSectionId);
  const [openBranchIds, setOpenBranchIds] = useState<Set<string>>(new Set());
  useEffect(() => {
    if (isMenuHomePath(pathname)) {
      const preferProfileHome = menuProfile.id !== 'admin' || menuProfile.homeMegaGroup === 'management';
      if (preferProfileHome) {
        setOpenMegaIds(new Set([menuProfile.homeMegaGroup]));
        setOpenSectionId(menuProfile.homeSectionId);
        return;
      }
    }

    if (activeMegaId) {
      setOpenMegaIds(new Set([activeMegaId]));
    }
  }, [activeMegaId, pathname, menuProfile]);

  useEffect(() => {
    if (activeSectionId) setOpenSectionId(activeSectionId);
  }, [activeSectionId]);

  useEffect(() => {
    if (activeBranchIds.size > 0) {
      setOpenBranchIds(new Set(activeBranchIds));
    }
  }, [activeBranchIds]);

  function toggleMega(id: MenuMegaGroupId) {
    setOpenMegaIds((current) => {
      if (current.has(id)) {
        setOpenSectionId(null);
        setOpenBranchIds(new Set());
        return new Set();
      }

      const mega = megaGroups.find((g) => g.meta.id === id);
      const activeInMega = mega?.sections.find((s) => sectionHasActive(pathname, s));
      setOpenSectionId(activeInMega?.id ?? null);
      setOpenBranchIds(activeInMega ? new Set(activeBranchIds) : new Set());
      return new Set([id]);
    });
  }

  function toggleSection(id: string) {
    setOpenSectionId((current) => {
      const next = current === id ? null : id;
      if (next !== current) setOpenBranchIds(new Set());
      return next;
    });
  }

  function toggleBranch(id: string, siblingIds: string[]) {
    setOpenBranchIds((current) => {
      if (current.has(id)) {
        const next = new Set(current);
        next.delete(id);
        for (const descendantId of branchDescendantMap.get(id) ?? []) {
          next.delete(descendantId);
        }
        return next;
      }

      const next = new Set(current);
      for (const siblingId of siblingIds) {
        if (siblingId === id) continue;
        next.delete(siblingId);
        for (const descendantId of branchDescendantMap.get(siblingId) ?? []) {
          next.delete(descendantId);
        }
      }
      next.add(id);
      return next;
    });
  }

  return (
    <div className="sidebar-mega-nav">
      <button type="button" className="sidebar-module-search" onClick={openModuleSearch}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="16" height="16" aria-hidden>
          <circle cx="11" cy="11" r="7" />
          <path d="M20 20l-3-3" />
        </svg>
        <span>Buscar módulo</span>
        <kbd>Ctrl+K</kbd>
      </button>

      <ProfileShortcuts label={menuProfile.label} shortcuts={menuProfile.shortcuts} />

      {megaGroups.map(({ meta, sections }) => {
        const megaActive = sections.some((s) => sectionHasActive(pathname, s));
        const megaExpanded = openMegaIds.has(meta.id);
        const isCollapsedDefault = menuProfile.collapsedByDefault.includes(meta.id);

        return (
          <div
            key={meta.id}
            className={`nav-mega-block${megaExpanded ? ' is-expanded' : ''}${isCollapsedDefault && !megaActive ? ' nav-mega-collapsed-default' : ''}`}
            data-mega={meta.id}
            style={{ '--mega-color': meta.color, '--mega-accent': meta.accent } as React.CSSProperties}
          >
            <MegaGroupHeader
              id={meta.id}
              title={meta.title}
              subtitle={meta.subtitle}
              color={meta.color}
              accent={meta.accent}
              icon={meta.icon}
              expanded={megaExpanded}
              onToggle={() => toggleMega(meta.id)}
              hasActive={megaActive}
            />
            {megaExpanded && (
              <div className="nav-mega-sections">
                {sections.map((section) => (
                  <NavCollapsible
                    key={section.id}
                    label={section.title}
                    icon={getSectionIcon(section.id)}
                    depth={0}
                    expanded={openSectionId === section.id}
                    onToggle={() => toggleSection(section.id)}
                    hasActive={activeSectionId === section.id}
                    className="nav-section-in-mega"
                    sectionId={section.id}
                  >
                    <MenuTree
                      nodes={section.nodes}
                      depth={1}
                      pathname={pathname}
                      openIds={openBranchIds}
                      onToggle={toggleBranch}
                    />
                  </NavCollapsible>
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
