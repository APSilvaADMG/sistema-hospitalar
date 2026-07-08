import type { UiDensity } from '../theme/appearanceConfig';
import { useAppearance } from '../theme/AppearanceProvider';

export function AppearanceSettings() {
  const { appearance, setDensity, resetAppearance } = useAppearance();

  return (
    <div className="appearance-settings">
      <section className="appearance-block card">
        <h3>Visual IASGH (Feegow)</h3>
        <p className="form-hint">
          O sistema utiliza o layout clínico Feegow com identidade IASGH: topbar azul, menu superior
          contextual e sidebar por módulo (Agenda, Financeiro, Guias, Comunicação).
        </p>
        <div className="appearance-swatch-row" aria-hidden style={{ marginTop: 12 }}>
          <span style={{ background: '#00b4fc', width: 48, height: 24, display: 'inline-block' }} />
          <span style={{ background: '#e8f7ff', width: 48, height: 24, display: 'inline-block' }} />
          <span style={{ background: '#2c3e50', width: 48, height: 24, display: 'inline-block' }} />
        </div>
      </section>

      <section className="appearance-block card">
        <h3>Densidade da interface</h3>
        <div className="appearance-toggle-row">
          {(['comfortable', 'compact'] as UiDensity[]).map((density) => (
            <button
              key={density}
              type="button"
              className={`appearance-pill${appearance.density === density ? ' active' : ''}`}
              onClick={() => setDensity(density)}
            >
              {density === 'comfortable' ? 'Confortável' : 'Compacta'}
            </button>
          ))}
        </div>
      </section>

      <div className="form-actions">
        <button type="button" className="btn btn-secondary" onClick={resetAppearance}>
          Restaurar padrão Feegow
        </button>
      </div>
    </div>
  );
}
