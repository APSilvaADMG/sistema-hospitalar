import { useMemo } from 'react';
import { PageHeader } from '../components/PageHeader';

type DownloadEntry = {
  id: string;
  fileName: string;
  type: string;
  generatedAt: string;
  status: 'Disponível' | 'Processando';
};

const sampleEntries: DownloadEntry[] = [
  { id: '1', fileName: 'relatorio_tpa_maio_2026.csv', type: 'TPA', generatedAt: '2026-06-16 14:22', status: 'Disponível' },
  { id: '2', fileName: 'folha_05_2026.xlsx', type: 'Folha', generatedAt: '2026-06-15 19:10', status: 'Disponível' },
  { id: '3', fileName: 'patologia_periodo.pdf', type: 'Patologia', generatedAt: '2026-06-14 08:40', status: 'Processando' },
];

export function DownloadsCenterPage() {
  const grouped = useMemo(() => {
    const map = new Map<string, DownloadEntry[]>();
    for (const item of sampleEntries) {
      const key = item.type;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
    }
    return Array.from(map.entries());
  }, []);

  return (
    <>
      <PageHeader eyebrow="Relatórios" title="Centro de download" subtitle="Arquivos exportados recentemente por módulo." />
      <div className="card-panel appt-panel">
        <div className="card-panel-header">Exportações recentes</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Arquivo</th><th>Módulo</th><th>Gerado em</th><th>Status</th></tr></thead>
            <tbody>
              {sampleEntries.map((entry) => (
                <tr key={entry.id}>
                  <td>{entry.fileName}</td>
                  <td>{entry.type}</td>
                  <td>{entry.generatedAt}</td>
                  <td>{entry.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="grid-2" style={{ marginTop: 16 }}>
        {grouped.map(([type, rows]) => (
          <div key={type} className="card">
            <h3 style={{ marginTop: 0 }}>{type}</h3>
            <p style={{ marginBottom: 0 }}>{rows.length} arquivo(s) no histórico.</p>
          </div>
        ))}
      </div>
    </>
  );
}

