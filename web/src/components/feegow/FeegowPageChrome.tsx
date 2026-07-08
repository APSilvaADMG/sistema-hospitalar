import type { ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { ContextualHelpButton } from '../help/ContextualHelpButton';
import { shouldShowContextualHelp } from '../../utils/helpContextRoutes';

type FeegowPageChromeProps = {
  title?: string;
  trail?: string;
  actions?: ReactNode;
  children: ReactNode;
  /** card = conteúdo em card branco; plain = só título + conteúdo direto */
  variant?: 'card' | 'plain';
};

export function FeegowPageChrome({
  title,
  trail,
  actions,
  children,
  variant = 'card',
}: FeegowPageChromeProps) {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const pageTitle = title ?? breadcrumb.title ?? 'Sistema';
  const pageTrail = trail ?? breadcrumb.section ?? '';
  const showContextHelp = shouldShowContextualHelp(pathname);

  return (
    <div className="feegow-page">
      <div className="feegow-page-head">
        <div className="feegow-page-head-left">
          <h1 className="feegow-page-title">{pageTitle}</h1>
          <p className="feegow-page-breadcrumb">
            <span className="feegow-crumb-home" aria-hidden>⌂</span>
            {pageTrail ? (
              <>
                <span className="feegow-crumb-sep">/</span>
                <span>{pageTrail}</span>
              </>
            ) : null}
          </p>
        </div>
        {(actions || showContextHelp) ? (
          <div className="feegow-page-head-actions">
            {showContextHelp ? <ContextualHelpButton route={pathname} /> : null}
            {actions}
          </div>
        ) : null}
      </div>
      <div className="feegow-page-body">
        {variant === 'card' ? (
          <div className="feegow-content-card">{children}</div>
        ) : (
          <div className="feegow-content-plain">{children}</div>
        )}
      </div>
    </div>
  );
}
