type Props = {
  title: string;
};

export function FeegowInventoryConfigPlaceholder({ title }: Props) {
  return (
    <div className="feegow-inventory-page">
      <header className="feegow-inventory-page-head">
        <div className="feegow-inventory-breadcrumb">
          <span>Estoque</span>
          <span className="feegow-inventory-crumb-sep">/</span>
          <span className="feegow-inventory-crumb-icon" aria-hidden>⚙</span>
          <span className="feegow-inventory-crumb-sep">/</span>
          <span>{title}</span>
        </div>
      </header>
      <section className="feegow-inventory-panel feegow-inventory-empty-panel">
        <p>Configuração em desenvolvimento.</p>
      </section>
    </div>
  );
}
