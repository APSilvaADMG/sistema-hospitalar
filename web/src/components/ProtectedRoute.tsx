import { Navigate, Outlet } from 'react-router-dom';

import { useAuth } from '../auth/AuthContext';



export function ProtectedRoute() {

  const { token, authReady } = useAuth();



  if (!authReady) {

    return (

      <div className="login-page login-page--bootstrapping">

        <div className="login-card card login-card--loading">

          <p>Carregando...</p>

        </div>

      </div>

    );

  }



  return token ? <Outlet /> : <Navigate to="/login" replace />;

}


