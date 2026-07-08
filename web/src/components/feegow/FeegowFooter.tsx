import { useEffect, useState } from 'react';
import {
  formatFooterDateTime,
  getInstitutionShortName,
  getTimeOfDayGreeting,
} from '../../config/iasghBranding';

export function FeegowFooter() {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const greeting = getTimeOfDayGreeting(now);
  const dateTimeLabel = formatFooterDateTime(now);
  const capitalizedDateTime = dateTimeLabel.charAt(0).toUpperCase() + dateTimeLabel.slice(1);

  return (
    <footer className="feegow-footer">
      <span className="feegow-footer-left">
        {getInstitutionShortName()} | {greeting}
      </span>
      <time className="feegow-footer-right" dateTime={now.toISOString()}>
        {capitalizedDateTime}
      </time>
    </footer>
  );
}
