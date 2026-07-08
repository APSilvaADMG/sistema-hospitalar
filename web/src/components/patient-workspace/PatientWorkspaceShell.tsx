import { useEffect, useState, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { api, type PatientDetailDto, type PatientDto } from '../../api/client';
import { usePatientWorkspace } from '../../hooks/usePatientWorkspace';
import type { PatientWorkspaceModuleId } from '../../navigation/patientWorkspaceConfig';
import { useAppearance } from '../../theme/AppearanceProvider';
import { isFeegowBrand } from '../../theme/appearanceConfig';
import { isFeegowDailyAgendaRoute, isFeegowPatientRoute } from '../../utils/feegowRoutes';
import { HubPatientPicker } from '../HubPatientPicker';
import { PatientContextHeader } from './PatientContextHeader';
import { PatientWorkspacePanels } from './PatientWorkspacePanels';
import './patient-workspace.css';

type Props = {
  moduleId: PatientWorkspaceModuleId;
  patients: PatientDto[];
  children?: ReactNode;
  /** Conteúdo operacional (fila, formulários) — exibido na aba quando `operationalViews` inclui a visão atual */
  operationalContent?: ReactNode;
  /** Slugs de aba do paciente que mostram `operationalContent` no lugar dos painéis */
  operationalViews?: string[];
  /** Ocultar picker quando paciente já vem da URL */
  hidePickerWhenSelected?: boolean;
  headerExtra?: ReactNode;
  /** Modo controlado (ex.: PEP com formulário acoplado ao paciente) */
  patientId?: string;
  onPatientIdChange?: (id: string) => void;
};

export function PatientWorkspaceShell({
  moduleId,
  patients,
  children,
  operationalContent,
  operationalViews,
  hidePickerWhenSelected = false,
  headerExtra,
  patientId: controlledPatientId,
  onPatientIdChange,
}: Props) {
  const { pathname } = useLocation();
  const { appearance } = useAppearance();
  const passthroughFeegowAgenda = isFeegowBrand(appearance.brand) && isFeegowDailyAgendaRoute(pathname);
  const passthroughFeegowPatient = isFeegowBrand(appearance.brand) && isFeegowPatientRoute(pathname);

  const workspace = usePatientWorkspace(moduleId);
  const patientId = controlledPatientId ?? workspace.patientId;
  const setPatientId = onPatientIdChange ?? workspace.setPatientId;
  const { patientView, setPatientView, patientTabs } = workspace;
  const hasPatient = Boolean(patientId);
  const [search, setSearch] = useState('');
  const [patient, setPatient] = useState<PatientDetailDto | null>(null);
  const [loadingPatient, setLoadingPatient] = useState(false);

  useEffect(() => {
    if (!patientId) {
      setPatient(null);
      return;
    }
    setLoadingPatient(true);
    api.getPatient(patientId)
      .then(setPatient)
      .catch(() => setPatient(null))
      .finally(() => setLoadingPatient(false));
  }, [patientId]);

  const showPicker = !passthroughFeegowAgenda && !passthroughFeegowPatient && !(hidePickerWhenSelected && hasPatient);
  const showOperationalInTab =
    hasPatient && Boolean(operationalContent) && operationalViews?.includes(patientView);

  if (passthroughFeegowAgenda || passthroughFeegowPatient) {
    return <>{children}</>;
  }

  return (
    <div className="patient-workspace-shell">
      {showPicker && (
        <HubPatientPicker
          patients={patients}
          patientId={patientId}
          search={search}
          onSearchChange={setSearch}
          onPatientChange={setPatientId}
        />
      )}

      {hasPatient && loadingPatient && (
        <p className="form-hint">Carregando paciente…</p>
      )}

      {hasPatient && patient && (
        <>
          <PatientContextHeader
            patient={patient}
            onClear={() => setPatientId('')}
            extra={headerExtra}
          />
          <nav className="patient-view-tabs" aria-label="Informações do paciente">
            {patientTabs.map((tab) => (
              <button
                key={tab.slug}
                type="button"
                className={`patient-view-tab${patientView === tab.slug ? ' active' : ''}`}
                onClick={() => setPatientView(tab.slug)}
              >
                {tab.label}
              </button>
            ))}
          </nav>
          {showOperationalInTab ? (
            <div className="patient-workspace-operational">{operationalContent}</div>
          ) : (
            <PatientWorkspacePanels patient={patient} moduleId={moduleId} view={patientView} />
          )}
        </>
      )}

      {!hasPatient && children}
    </div>
  );
}
