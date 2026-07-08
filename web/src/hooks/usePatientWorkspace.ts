import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  PATIENT_QUERY_KEY,
  PATIENT_VIEW_QUERY_KEY,
  patientWorkspaceTabs,
  type PatientWorkspaceModuleId,
} from '../navigation/patientWorkspaceConfig';

export function usePatientWorkspace(moduleId: PatientWorkspaceModuleId) {
  const [searchParams, setSearchParams] = useSearchParams();

  const patientId = searchParams.get(PATIENT_QUERY_KEY) ?? '';
  const defaultView = patientWorkspaceTabs[moduleId][0]?.slug ?? 'resumo';
  const patientView = searchParams.get(PATIENT_VIEW_QUERY_KEY) ?? defaultView;

  const setPatientId = useCallback((id: string) => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (id) {
        next.set(PATIENT_QUERY_KEY, id);
        if (!next.get(PATIENT_VIEW_QUERY_KEY)) {
          next.set(PATIENT_VIEW_QUERY_KEY, defaultView);
        }
      } else {
        next.delete(PATIENT_QUERY_KEY);
        next.delete(PATIENT_VIEW_QUERY_KEY);
      }
      return next;
    }, { replace: true });
  }, [setSearchParams, defaultView]);

  const setPatientView = useCallback((view: string) => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (patientId) {
        next.set(PATIENT_VIEW_QUERY_KEY, view);
      }
      return next;
    }, { replace: true });
  }, [setSearchParams, patientId]);

  const tabs = useMemo(() => patientWorkspaceTabs[moduleId], [moduleId]);

  return {
    patientId,
    patientView,
    setPatientId,
    setPatientView,
    patientTabs: tabs,
    hasPatient: Boolean(patientId),
  };
}
