import { Route, Routes } from 'react-router-dom';

import { AuthProvider } from './auth/AuthContext';

import { Layout } from './components/Layout';

import { ProtectedRoute } from './components/ProtectedRoute';

import { buildAppMenuRoutes } from './navigation/AppMenuRoutes';
import { DashboardPage } from './pages/DashboardPage';
import { MedicalRecordPage } from './pages/MedicalRecordPage';
import { LoginRouter } from './pages/LoginRouter';
import { MedicationsBulaPage } from './pages/MedicationsBulaPage';

import { PatientPortalPage } from './pages/PatientPortalPage';
import { ParkingPage } from './pages/ParkingPage';
import { HospitalityPage } from './pages/HospitalityPage';
import { lazyPage } from './navigation/lazyPage';

const TvPlayerPage = lazyPage(() => import('./pages/tv/TvPlayerPage').then((m) => ({ default: m.TvPlayerPage })));

export default function App() {

  return (

    <AuthProvider>

      <Routes>

        <Route path="/login" element={<LoginRouter />} />

        <Route path="/tv/:slug" element={<TvPlayerPage />} />

        <Route element={<ProtectedRoute />}>

          <Route element={<Layout />}>

            <Route index element={<DashboardPage />} />

            <Route path="pacientes/:patientId/prontuario/:section?" element={<MedicalRecordPage />} />

            <Route path="portal-paciente" element={<PatientPortalPage />} />



            {/* Rotas legadas fora do menu enxuto */}
            <Route path="medicamentos" element={<MedicationsBulaPage />} />
            <Route path="estacionamento" element={<ParkingPage />} />
            <Route path="hospedagem-interna" element={<HospitalityPage />} />

            {buildAppMenuRoutes()}

          </Route>

        </Route>

      </Routes>

    </AuthProvider>

  );

}

