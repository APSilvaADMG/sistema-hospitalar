import type { ReactNode } from 'react';

import { useAppearance } from '../theme/AppearanceProvider';

import { isFeegowBrand } from '../theme/appearanceConfig';



type PageHeaderProps = {

  eyebrow?: string;

  title: string;

  subtitle?: string;

  children?: ReactNode;

};



/**

 * Cabeçalho interno legado (Bayanno).

 * No shell IASGH/Feegow o título fica no FeegowPageChrome — aqui só renderiza ações.

 */

export function PageHeader({ title, subtitle, children }: PageHeaderProps) {

  const { appearance } = useAppearance();

  const iasghShell = isFeegowBrand(appearance.brand);



  if (iasghShell) {

    if (!children) return null;

    return (

      <div className="feegow-module-actions no-print" style={{ marginBottom: 12, textAlign: 'right' }}>

        {children}

      </div>

    );

  }



  return (

    <div className="box-header bayanno-page-header">

      <span className="title">

        <i className="icon-reorder" aria-hidden />

        {' '}

        {title}

      </span>

      {subtitle ? <span className="bayanno-page-subtitle">{subtitle}</span> : null}

      {children ? (

        <ul className="box-toolbar bayanno-page-toolbar">

          <li className="toolbar-link">{children}</li>

        </ul>

      ) : null}

    </div>

  );

}


