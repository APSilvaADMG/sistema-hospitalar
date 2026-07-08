import { Link } from 'react-router-dom';
import { FeegowPatientScreenLayout } from '../components/feegow/patients/FeegowPatientScreenLayout';
import { FeegowPatientSidebar } from '../components/feegow/patients/FeegowPatientSidebar';
import { feegowPatientInsertPath, feegowPatientNavLabel } from '../components/feegow/patients/feegowPatientNav';

type Props = {
  section: string;
};

export function FeegowPatientSectionGatePage({ section }: Props) {
  const label = feegowPatientNavLabel(section);

  return (
    <FeegowPatientScreenLayout sidebar={<FeegowPatientSidebar mode="insert" activeSection={section} />}>
      <div className="feegow-patient-card feegow-patient-gate-card">
        <header className="feegow-patient-card-head">
          <div className="feegow-patient-breadcrumb">
            <span className="feegow-patient-crumb-label">{label}</span>
          </div>
        </header>
        <div className="feegow-patient-gate-body">
          <p>Cadastre o paciente em <strong>Dados Principais</strong> antes de acessar esta seção.</p>
          <Link to={feegowPatientInsertPath('dados-principais')} className="feegow-patient-gate-link">
            Ir para Dados Principais
          </Link>
        </div>
      </div>
    </FeegowPatientScreenLayout>
  );
}
