import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  financialCategoryLabel,
  type PayableCategoryPresetDto,
} from '../../../api/client';
import { FeegowFinancePageHead } from './FeegowFinancePageHead';
import { feegowFinanceInsertPath } from './feegowFinanceNav';

function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function FeegowFinanceFixedExpenses() {
  const [presets, setPresets] = useState<PayableCategoryPresetDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setPresets(await api.getPayableCategoryPresets());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar despesas fixas.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div className="feegow-finance-page">
      <FeegowFinancePageHead title="Despesas Fixas" />
      <p className="feegow-finance-lead" style={{ marginTop: 0, color: 'var(--muted)' }}>
        Modelos recorrentes para aluguel, utilidades, folha e demais despesas operacionais.
      </p>

      <section className="feegow-finance-panel">
        {error && <div className="alert alert-error">{error}</div>}
        {loading && <p>Carregando modelos…</p>}

        <div className="guides-table-wrap">
          <div className="guides-table-scroll">
            <table className="guides-data-table">
              <thead>
                <tr>
                  <th>Categoria</th>
                  <th>Descrição sugerida</th>
                  <th>Valor ref.</th>
                  <th>Vencimento (dias)</th>
                  <th>Ação</th>
                </tr>
              </thead>
              <tbody>
                {presets.map((preset) => (
                  <tr key={preset.label}>
                    <td>{financialCategoryLabel(preset.category)}</td>
                    <td>{preset.descriptionTemplate}</td>
                    <td>{formatCurrency(preset.suggestedAmount)}</td>
                    <td>{preset.suggestedDueDays}</td>
                    <td>
                      <Link
                        to={`${feegowFinanceInsertPath('pagar')}?categoria=${financialCategoryLabel(preset.category)}`}
                        className="btn btn-secondary btn-sm"
                      >
                        Lançar
                      </Link>
                    </td>
                  </tr>
                ))}
                {presets.length === 0 && !loading && (
                  <tr>
                    <td colSpan={5} className="guides-table-empty">Nenhum modelo cadastrado.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </section>
    </div>
  );
}
