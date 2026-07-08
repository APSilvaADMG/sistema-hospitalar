import type { ReactNode } from 'react';
import { useAuth } from '../auth/AuthContext';

type PermissionGateProps = {
  permissions: string[];
  requireAll?: boolean;
  fallback?: ReactNode;
  children: ReactNode;
};

export function PermissionGate({
  permissions,
  requireAll = false,
  fallback = null,
  children,
}: PermissionGateProps) {
  const { hasPermission } = useAuth();

  const allowed = requireAll
    ? permissions.every((p) => hasPermission(p))
    : hasPermission(...permissions);

  if (!allowed) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
