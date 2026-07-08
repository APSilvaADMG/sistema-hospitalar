import { lazy, Suspense, type ComponentType } from 'react';

function PageLoadingFallback() {
  return (
    <div className="page-content" style={{ padding: '2rem', color: 'var(--muted)' }}>
      Carregando…
    </div>
  );
}

export function lazyPage(
  factory: () => Promise<{ default: ComponentType } | Record<string, ComponentType>>,
  exportName = 'default',
) {
  const Lazy = lazy(async () => {
    const module = await factory();
    const Component = exportName === 'default'
      ? (module as { default: ComponentType }).default
      : (module as Record<string, ComponentType>)[exportName];
    return { default: Component };
  });

  return function LazyPageWrapper() {
    return (
      <Suspense fallback={<PageLoadingFallback />}>
        <Lazy />
      </Suspense>
    );
  };
}
