import { useEffect, useState } from 'react';
import { useAppearance } from '../theme/AppearanceProvider';

const BANNER_DISMISS_KEY = 'hms-banner-dismissed';

export function SystemAlertBanner() {
  const { appearance } = useAppearance();
  const [dismissed, setDismissed] = useState(() => {
    try {
      return sessionStorage.getItem(BANNER_DISMISS_KEY) === '1';
    } catch {
      return false;
    }
  });

  useEffect(() => {
    if (!appearance.showTestBanner) {
      setDismissed(false);
    }
  }, [appearance.showTestBanner]);

  if (!appearance.showTestBanner || dismissed) {
    return null;
  }

  function handleDismiss() {
    setDismissed(true);
    try {
      sessionStorage.setItem(BANNER_DISMISS_KEY, '1');
    } catch {
      /* ignore */
    }
  }

  return (
    <div className="system-alert-banner" role="status">
      <p className="system-alert-banner-text">{appearance.bannerMessage}</p>
      <button
        type="button"
        className="system-alert-banner-close"
        onClick={handleDismiss}
        aria-label="Fechar aviso"
      >
        ×
      </button>
    </div>
  );
}
