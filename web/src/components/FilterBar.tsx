import type { FormEvent, ReactNode } from 'react';

type FilterBarProps = {
  children: ReactNode;
  onSubmit?: (e: FormEvent) => void;
  actions?: ReactNode;
};

export function FilterBar({ children, onSubmit, actions }: FilterBarProps) {
  const content = (
    <>
      <div className="filter-bar-fields">{children}</div>
      {actions ? <div className="filter-bar-actions">{actions}</div> : null}
    </>
  );

  const className = 'filter-bar bayanno-filter-toolbar';

  if (onSubmit) {
    return (
      <form className={className} onSubmit={onSubmit}>
        {content}
      </form>
    );
  }

  return <div className={className}>{content}</div>;
}
