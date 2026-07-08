import { useMemo } from 'react';
import { Modal } from '../Modal';
import type { HealthInsuranceDto, PatientDto } from '../../api/client';
import { FuniConsultationGuideForm } from './FuniConsultationGuideForm';
import { FuniChemotherapyGuideForm } from './FuniChemotherapyGuideForm';
import { FuniRadiotherapyGuideForm } from './FuniRadiotherapyGuideForm';
import { TissGuideCaptureForm } from '../clinical/TissGuideCaptureForm';
import { ReportClinicalCaptureForm } from '../clinical/ReportClinicalCaptureForm';
import {
  type ClinicalDocumentContext,
  guideTypeLabel,
  isFuniFormGuideType,
} from '../../utils/clinicalDocumentWorkflow';

type Props = {
  open: boolean;
  onClose: () => void;
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  patientId: string;
  clinicalContext: ClinicalDocumentContext;
  guideType?: number;
  reportCode?: string;
  reportName?: string;
  onSaved?: () => void;
};

export function ClinicalGuideCaptureModal({
  open,
  onClose,
  guideType = 1,
  reportCode,
  reportName,
  patients,
  insurances,
  patientId,
  clinicalContext,
  onSaved,
}: Props) {
  const title = useMemo(() => {
    if (reportCode) return `Dados clínicos — ${reportName ?? reportCode}`;
    return `Dados clínicos — ${guideTypeLabel(guideType)}`;
  }, [guideType, reportCode, reportName]);

  const formProps = {
    patients,
    insurances,
    workflow: 'clinical' as const,
    clinicalContext,
    lockedPatientId: patientId,
    onClinicalSaved: () => {
      onSaved?.();
      onClose();
    },
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      subtitle="Preencha durante o atendimento. A guia ou relatório será gerado depois no faturamento."
      width="lg"
    >
      <div className="funi-clinical-capture-modal">
        {reportCode ? (
          <ReportClinicalCaptureForm
            reportCode={reportCode}
            reportName={reportName}
            patients={patients}
            workflow="clinical"
            clinicalContext={clinicalContext}
            lockedPatientId={patientId}
            onClinicalSaved={formProps.onClinicalSaved}
          />
        ) : guideType === 1 ? (
          <FuniConsultationGuideForm {...formProps} />
        ) : guideType === 17 ? (
          <FuniChemotherapyGuideForm {...formProps} />
        ) : guideType === 18 ? (
          <FuniRadiotherapyGuideForm {...formProps} />
        ) : (
          <TissGuideCaptureForm
            guideType={guideType}
            guideTitle={guideTypeLabel(guideType)}
            {...formProps}
          />
        )}
      </div>
    </Modal>
  );
}

export { isFuniFormGuideType };
