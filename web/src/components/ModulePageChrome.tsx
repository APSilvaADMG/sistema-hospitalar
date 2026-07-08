import type { ReactNode } from 'react';
import { ModuleNav } from './ModuleNav';
import { PageHeader } from './PageHeader';
import type { ModuleContextId } from '../navigation/contextualModules';
import type { ModuleTab } from '../navigation/useModuleSection';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';

type Props = {
  embedded?: boolean;
  eyebrow?: string;
  title: string;
  subtitle?: string;
  basePath?: string;
  tabs?: ModuleTab[];
  contextId?: ModuleContextId;
  actions?: ReactNode;
  children: ReactNode;
};

/** Cabeçalho e abas do módulo — omitidos no modo embutido (hub central). */
export function ModulePageChrome({
  embedded = false,
  eyebrow,
  title,
  subtitle,
  basePath,
  tabs,
  contextId,
  actions,
  children,
}: Props) {
  const { appearance } = useAppearance();
  const iasghShell = isFeegowBrand(appearance.brand);

  if (embedded) {
    return <>{children}</>;
  }

  const hasModuleNav = Boolean(basePath && tabs);

  return (
    <>
      {!iasghShell && !hasModuleNav && (
        <PageHeader eyebrow={eyebrow} title={title} subtitle={subtitle}>
          {actions}
        </PageHeader>
      )}
      {(hasModuleNav || iasghShell) && actions ? (
        <div className="feegow-module-actions no-print" style={{ marginBottom: 8, textAlign: 'right' }}>
          {actions}
        </div>
      ) : null}
      {hasModuleNav && (
        <ModuleNav basePath={basePath!} tabs={tabs!} contextId={contextId} />
      )}
      {children}
    </>
  );
}
