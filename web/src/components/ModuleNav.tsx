import { ModuleSectionBanner } from './ModuleSectionBanner';
import { ModuleTabs } from './ModuleTabs';
import { ModuleContextTools } from './ModuleContextTools';
import type { ModuleContextId } from '../navigation/contextualModules';
import type { ModuleTab } from '../navigation/useModuleSection';

type Props = {
  basePath: string;
  tabs: ModuleTab[];
  contextId?: ModuleContextId;
};

export function ModuleNav({ basePath, tabs, contextId }: Props) {
  return (
    <>
      <ModuleTabs basePath={basePath} tabs={tabs} />
      {contextId ? (
        <div id="modulo-atalhos" className="module-nav-atalhos">
          <ModuleContextTools contextId={contextId} />
        </div>
      ) : null}
      <ModuleSectionBanner basePath={basePath} />
    </>
  );
}
