import { type ReactNode } from 'react';

type OpsDashKpiTone = 'green' | 'teal' | 'yellow' | 'red' | 'neutral';

type OpsDashKpiProps = {
  value: string | number;
  label: string;
  tone?: OpsDashKpiTone;
  footer?: ReactNode;
};

export function OpsDashKpi({ value, label, tone = 'neutral', footer }: OpsDashKpiProps) {
  return (
    <div className={`feegow-dash-kpi feegow-dash-kpi-${tone}`}>
      <div className="feegow-dash-kpi-value">{value}</div>
      <div className="feegow-dash-kpi-label">{label}</div>
      {footer ? <div className="feegow-dash-kpi-footer">{footer}</div> : null}
    </div>
  );
}

export function occupancyRateTone(rate: number): OpsDashKpiTone {
  if (rate >= 90) return 'red';
  if (rate >= 75) return 'yellow';
  return 'green';
}

export function occupancyRateCssClass(rate: number): string {
  if (rate >= 90) return 'ops-dash-occupancy-high';
  if (rate >= 75) return 'ops-dash-occupancy-mid';
  return 'ops-dash-occupancy-ok';
}
