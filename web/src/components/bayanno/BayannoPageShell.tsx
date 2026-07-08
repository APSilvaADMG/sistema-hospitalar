import type { ReactNode } from 'react';

type BayannoPageShellProps = {
  children: ReactNode;
  /** Dashboard e login não usam o box padrão. */
  bare?: boolean;
};

/**
 * Envelope padrão das telas internas Bayanno (container-fluid + box).
 * Equivalente ao que o index.php envolve em cada view PHP.
 */
export function BayannoPageShell({ children, bare = false }: BayannoPageShellProps) {
  if (bare) {
    return <div className="bayanno-php-screen bayanno-page-bare">{children}</div>;
  }

  return (
    <div className="bayanno-php-screen">
      <div className="container-fluid padded">
        <div className="box bayanno-page-box">
          <div className="box-content padded bayanno-page-inner">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}
