type KpiVariant = 'default' | 'primary' | 'info' | 'success' | 'warning' | 'danger' | 'neutral';

type KpiCardProps = {
  label: string;
  value: string | number;
  variant?: KpiVariant;
};

export function KpiCard({ label, value, variant = 'default' }: KpiCardProps) {
  return (
    <div className={`kpi-card kpi-${variant}`}>
      <span className="kpi-label">{label}</span>
      <span className="kpi-value">{value}</span>
    </div>
  );
}
