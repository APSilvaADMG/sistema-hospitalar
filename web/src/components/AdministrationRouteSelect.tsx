import type { AdministrationRouteDto } from '../api/client';
import { formatRouteLabel } from '../utils/prescriptionFormat';

type Props = {
  routes: AdministrationRouteDto[];
  value: string;
  onChange: (code: string) => void;
  disabled?: boolean;
  required?: boolean;
  className?: string;
  placeholder?: string;
};

export function AdministrationRouteSelect({
  routes,
  value,
  onChange,
  disabled,
  required,
  className,
  placeholder = 'Selecione a via',
}: Props) {
  return (
    <select
      className={className}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled || routes.length === 0}
      required={required}
    >
      <option value="">{routes.length === 0 ? 'Carregando vias…' : placeholder}</option>
      {routes.map((route) => (
        <option key={route.code} value={route.code}>
          {formatRouteLabel(route)}
        </option>
      ))}
    </select>
  );
}
