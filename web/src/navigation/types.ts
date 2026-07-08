export type MenuRoles = 'staff' | 'adminOrReception' | 'admin' | 'doctor' | 'securityLgpd';

export type MenuMegaGroupId = 'clinical' | 'diagnostic' | 'administrative' | 'management' | 'security';

export type MenuLeaf = {
  id: string;
  label: string;
  path: string;
  end?: boolean;
  badge?: number;
  roles?: MenuRoles[];
  /** Qualquer permissão listada libera o item. */
  permissions?: string[];
};

export type MenuBranch = {
  id: string;
  label: string;
  children: MenuNode[];
  roles?: MenuRoles[];
};

export type MenuNode = MenuLeaf | MenuBranch;

export type MenuSection = {
  id: string;
  title: string;
  megaGroup: MenuMegaGroupId;
  nodes: MenuNode[];
  roles?: MenuRoles[];
  permissions?: string[];
};

export function isMenuBranch(node: MenuNode): node is MenuBranch {
  return 'children' in node;
}

export function flattenMenuLeaves(nodes: MenuNode[]): MenuLeaf[] {
  const leaves: MenuLeaf[] = [];
  for (const node of nodes) {
    if (isMenuBranch(node)) {
      leaves.push(...flattenMenuLeaves(node.children));
    } else {
      leaves.push(node);
    }
  }
  return leaves;
}
