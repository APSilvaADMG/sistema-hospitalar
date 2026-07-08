/** Gerado por npm run sync:bayanno-catalog — não editar manualmente. */

export type BayannoNavLink = {
  type: 'link';
  route: string;
  label: string;
  labelKey: string;
  icon: string;
  path: string;
};

export type BayannoNavSubmenu = {
  type: 'submenu';
  label: string;
  labelKey: string;
  icon: string;
  submenuId: string;
  children: BayannoNavLink[];
};

export type BayannoNavItem = BayannoNavLink | BayannoNavSubmenu;

export type BayannoRoleNav = {
  role: string;
  roleLabel: string;
  items: BayannoNavItem[];
};

export const BAYANNO_ROLE_LABELS: Record<string, string> = {
  "accountant": "Contador",
  "admin": "Administrador",
  "doctor": "Médico",
  "laboratorist": "Laboratorista",
  "nurse": "Enfermagem",
  "patient": "Paciente",
  "pharmacist": "Farmacêutico",
  "system": "Sistema"
};

export const BAYANNO_SGHC_NAV: BayannoRoleNav[] = [
  {
    "role": "accountant",
    "roleLabel": "Contador",
    "items": [
      {
        "type": "link",
        "route": "accountant/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/accountant/dashboard"
      },
      {
        "type": "link",
        "route": "accountant/manage_invoice",
        "label": "Faturas / receber pagamento",
        "labelKey": "invoice / take_payment",
        "icon": "icon-list-alt icon-2x",
        "path": "/sghc/accountant/manage_invoice"
      },
      {
        "type": "link",
        "route": "accountant/view_payment",
        "label": "Pagamentos",
        "labelKey": "view_payment",
        "icon": "icon-money icon-2x",
        "path": "/sghc/accountant/view_payment"
      },
      {
        "type": "link",
        "route": "accountant/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/accountant/manage_profile"
      }
    ]
  },
  {
    "role": "admin",
    "roleLabel": "Administrador",
    "items": [
      {
        "type": "link",
        "route": "admin/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/admin/dashboard"
      },
      {
        "type": "link",
        "route": "admin/manage_department",
        "label": "Departamentos",
        "labelKey": "department",
        "icon": "icon-sitemap icon-2x",
        "path": "/sghc/admin/manage_department"
      },
      {
        "type": "link",
        "route": "admin/manage_doctor",
        "label": "Médicos",
        "labelKey": "doctor",
        "icon": "icon-user-md icon-2x",
        "path": "/sghc/admin/manage_doctor"
      },
      {
        "type": "link",
        "route": "admin/manage_patient",
        "label": "Pacientes",
        "labelKey": "patient",
        "icon": "icon-user icon-2x",
        "path": "/sghc/admin/manage_patient"
      },
      {
        "type": "link",
        "route": "admin/manage_nurse",
        "label": "Enfermeiros",
        "labelKey": "nurse",
        "icon": "icon-plus-sign-alt icon-2x",
        "path": "/sghc/admin/manage_nurse"
      },
      {
        "type": "link",
        "route": "admin/manage_pharmacist",
        "label": "Farmacêuticos",
        "labelKey": "pharmacist",
        "icon": "icon-medkit icon-2x",
        "path": "/sghc/admin/manage_pharmacist"
      },
      {
        "type": "link",
        "route": "admin/manage_laboratorist",
        "label": "Laboratoristas",
        "labelKey": "laboratorist",
        "icon": "icon-beaker icon-2x",
        "path": "/sghc/admin/manage_laboratorist"
      },
      {
        "type": "link",
        "route": "admin/manage_accountant",
        "label": "Contadores",
        "labelKey": "accountant",
        "icon": "icon-money icon-2x",
        "path": "/sghc/admin/manage_accountant"
      },
      {
        "type": "submenu",
        "label": "Monitor hospitalar",
        "labelKey": "monitor_hospital",
        "icon": "icon-screenshot icon-2x",
        "submenuId": "view_hospital_submenu",
        "children": []
      },
      {
        "type": "link",
        "route": "admin/view_appointment",
        "label": "Agendamentos",
        "labelKey": "view_appointment",
        "icon": "icon-exchange",
        "path": "/sghc/admin/view_appointment"
      },
      {
        "type": "link",
        "route": "admin/view_payment",
        "label": "Pagamentos",
        "labelKey": "view_payment",
        "icon": "icon-money",
        "path": "/sghc/admin/view_payment"
      },
      {
        "type": "link",
        "route": "admin/view_bed_status",
        "label": "Status de leitos",
        "labelKey": "view_bed_status",
        "icon": "icon-hdd",
        "path": "/sghc/admin/view_bed_status"
      },
      {
        "type": "link",
        "route": "admin/view_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "view_blood_bank",
        "icon": "icon-tint",
        "path": "/sghc/admin/view_blood_bank"
      },
      {
        "type": "link",
        "route": "admin/view_medicine",
        "label": "Medicamentos",
        "labelKey": "view_medicine",
        "icon": "icon-medkit",
        "path": "/sghc/admin/view_medicine"
      },
      {
        "type": "link",
        "route": "admin/view_report/operation",
        "label": "Relatório cirúrgico",
        "labelKey": "view_operation",
        "icon": "icon-reorder",
        "path": "/sghc/admin/view_report/operation"
      },
      {
        "type": "link",
        "route": "admin/view_report/birth",
        "label": "Relatório de nascimento",
        "labelKey": "view_birth_report",
        "icon": "icon-github-alt",
        "path": "/sghc/admin/view_report/birth"
      },
      {
        "type": "link",
        "route": "admin/view_report/death",
        "label": "Relatório de óbito",
        "labelKey": "view_death_report",
        "icon": "icon-user",
        "path": "/sghc/admin/view_report/death"
      },
      {
        "type": "submenu",
        "label": "Configurações",
        "labelKey": "settings",
        "icon": "icon-wrench icon-2x",
        "submenuId": "settings_submenu",
        "children": []
      },
      {
        "type": "link",
        "route": "admin/manage_email_template",
        "label": "Modelos de e-mail",
        "labelKey": "manage_email_template",
        "icon": "icon-envelope",
        "path": "/sghc/admin/manage_email_template"
      },
      {
        "type": "link",
        "route": "admin/manage_noticeboard",
        "label": "Mural de avisos",
        "labelKey": "manage_noticeboard",
        "icon": "icon-columns",
        "path": "/sghc/admin/manage_noticeboard"
      },
      {
        "type": "link",
        "route": "admin/system_settings",
        "label": "Configurações do sistema",
        "labelKey": "system_settings",
        "icon": "icon-h-sign",
        "path": "/sghc/admin/system_settings"
      },
      {
        "type": "link",
        "route": "admin/manage_language",
        "label": "Idiomas",
        "labelKey": "manage_language",
        "icon": "icon-globe",
        "path": "/sghc/admin/manage_language"
      },
      {
        "type": "link",
        "route": "admin/backup_restore",
        "label": "Backup e restauração",
        "labelKey": "backup_restore",
        "icon": "icon-download-alt",
        "path": "/sghc/admin/backup_restore"
      },
      {
        "type": "link",
        "route": "admin/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/admin/manage_profile"
      }
    ]
  },
  {
    "role": "doctor",
    "roleLabel": "Médico",
    "items": [
      {
        "type": "link",
        "route": "doctor/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/doctor/dashboard"
      },
      {
        "type": "link",
        "route": "doctor/manage_patient",
        "label": "Pacientes",
        "labelKey": "patient",
        "icon": "icon-user icon-2x",
        "path": "/sghc/doctor/manage_patient"
      },
      {
        "type": "link",
        "route": "doctor/manage_appointment",
        "label": "Agendamentos",
        "labelKey": "manage_appointment",
        "icon": "icon-edit icon-2x",
        "path": "/sghc/doctor/manage_appointment"
      },
      {
        "type": "link",
        "route": "doctor/manage_prescription",
        "label": "Prescrições",
        "labelKey": "manage_prescription",
        "icon": "icon-stethoscope icon-2x",
        "path": "/sghc/doctor/manage_prescription"
      },
      {
        "type": "link",
        "route": "doctor/manage_bed_allotment",
        "label": "Alocação de leitos",
        "labelKey": "bed_allotment",
        "icon": "icon-hdd icon-2x",
        "path": "/sghc/doctor/manage_bed_allotment"
      },
      {
        "type": "link",
        "route": "doctor/view_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "view_blood_bank",
        "icon": "icon-tint icon-2x",
        "path": "/sghc/doctor/view_blood_bank"
      },
      {
        "type": "link",
        "route": "doctor/manage_report",
        "label": "Relatórios clínicos",
        "labelKey": "manage_report",
        "icon": "icon-hospital icon-2x",
        "path": "/sghc/doctor/manage_report"
      },
      {
        "type": "link",
        "route": "doctor/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/doctor/manage_profile"
      }
    ]
  },
  {
    "role": "laboratorist",
    "roleLabel": "Laboratorista",
    "items": [
      {
        "type": "link",
        "route": "laboratorist/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/laboratorist/dashboard"
      },
      {
        "type": "link",
        "route": "laboratorist/manage_prescription",
        "label": "Laudos de diagnóstico",
        "labelKey": "add_diagnosis_report",
        "icon": "icon-stethoscope icon-2x",
        "path": "/sghc/laboratorist/manage_prescription"
      },
      {
        "type": "link",
        "route": "laboratorist/manage_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "manage_blood_bank",
        "icon": "icon-tint icon-2x",
        "path": "/sghc/laboratorist/manage_blood_bank"
      },
      {
        "type": "link",
        "route": "laboratorist/manage_blood_donor",
        "label": "Doadores de sangue",
        "labelKey": "manage_blood_donor",
        "icon": "icon-user icon-2x",
        "path": "/sghc/laboratorist/manage_blood_donor"
      },
      {
        "type": "link",
        "route": "laboratorist/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/laboratorist/manage_profile"
      }
    ]
  },
  {
    "role": "nurse",
    "roleLabel": "Enfermagem",
    "items": [
      {
        "type": "link",
        "route": "nurse/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/nurse/dashboard"
      },
      {
        "type": "link",
        "route": "nurse/manage_patient",
        "label": "Pacientes",
        "labelKey": "patient",
        "icon": "icon-user icon-2x",
        "path": "/sghc/nurse/manage_patient"
      },
      {
        "type": "submenu",
        "label": "Bed Ward",
        "labelKey": "bed_ward",
        "icon": "icon-hdd icon-2x",
        "submenuId": "bed_submenu",
        "children": []
      },
      {
        "type": "link",
        "route": "nurse/manage_bed",
        "label": "Leitos",
        "labelKey": "manage_bed",
        "icon": "icon-hdd",
        "path": "/sghc/nurse/manage_bed"
      },
      {
        "type": "link",
        "route": "nurse/manage_bed_allotment",
        "label": "Alocação de leitos",
        "labelKey": "manage_bed_allotment",
        "icon": "icon-wrench",
        "path": "/sghc/nurse/manage_bed_allotment"
      },
      {
        "type": "submenu",
        "label": "Banco de sangue",
        "labelKey": "blood_bank",
        "icon": "icon-tint icon-2x",
        "submenuId": "blood_submenu",
        "children": []
      },
      {
        "type": "link",
        "route": "nurse/manage_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "manage_blood_bank",
        "icon": "icon-tint",
        "path": "/sghc/nurse/manage_blood_bank"
      },
      {
        "type": "link",
        "route": "nurse/manage_blood_donor",
        "label": "Doadores de sangue",
        "labelKey": "manage_blood_donor",
        "icon": "icon-user",
        "path": "/sghc/nurse/manage_blood_donor"
      },
      {
        "type": "link",
        "route": "nurse/manage_report",
        "label": "Relatórios",
        "labelKey": "report",
        "icon": "icon-hospital icon-2x",
        "path": "/sghc/nurse/manage_report"
      },
      {
        "type": "link",
        "route": "nurse/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/nurse/manage_profile"
      }
    ]
  },
  {
    "role": "patient",
    "roleLabel": "Paciente",
    "items": [
      {
        "type": "link",
        "route": "patient/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/patient/dashboard"
      },
      {
        "type": "link",
        "route": "patient/view_appointment",
        "label": "Agendamentos",
        "labelKey": "view_appointment",
        "icon": "icon-edit icon-2x",
        "path": "/sghc/patient/view_appointment"
      },
      {
        "type": "link",
        "route": "patient/view_prescription",
        "label": "Prescrições",
        "labelKey": "view_prescription",
        "icon": "icon-stethoscope icon-2x",
        "path": "/sghc/patient/view_prescription"
      },
      {
        "type": "link",
        "route": "patient/view_doctor",
        "label": "Médicos",
        "labelKey": "view_doctor",
        "icon": "icon-user-md icon-2x",
        "path": "/sghc/patient/view_doctor"
      },
      {
        "type": "link",
        "route": "patient/view_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "view_blood_bank",
        "icon": "icon-tint icon-2x",
        "path": "/sghc/patient/view_blood_bank"
      },
      {
        "type": "link",
        "route": "patient/view_admit_history",
        "label": "Histórico de internação",
        "labelKey": "admit_history",
        "icon": "icon-hdd icon-2x",
        "path": "/sghc/patient/view_admit_history"
      },
      {
        "type": "link",
        "route": "patient/view_operation_history",
        "label": "Histórico cirúrgico",
        "labelKey": "operation_history",
        "icon": "icon-hospital icon-2x",
        "path": "/sghc/patient/view_operation_history"
      },
      {
        "type": "link",
        "route": "patient/view_invoice",
        "label": "Faturas",
        "labelKey": "view_invoice",
        "icon": "icon-credit-card icon-2x",
        "path": "/sghc/patient/view_invoice"
      },
      {
        "type": "link",
        "route": "patient/payment_history",
        "label": "Histórico de pagamentos",
        "labelKey": "payment_history",
        "icon": "icon-money icon-2x",
        "path": "/sghc/patient/payment_history"
      },
      {
        "type": "link",
        "route": "patient/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/patient/manage_profile"
      }
    ]
  },
  {
    "role": "pharmacist",
    "roleLabel": "Farmacêutico",
    "items": [
      {
        "type": "link",
        "route": "pharmacist/dashboard",
        "label": "Painel",
        "labelKey": "dashboard",
        "icon": "icon-desktop icon-2x",
        "path": "/sghc/pharmacist/dashboard"
      },
      {
        "type": "link",
        "route": "pharmacist/manage_medicine_category",
        "label": "Categorias de medicamento",
        "labelKey": "medicine_category",
        "icon": "icon-edit icon-2x",
        "path": "/sghc/pharmacist/manage_medicine_category"
      },
      {
        "type": "link",
        "route": "pharmacist/manage_medicine",
        "label": "Medicamentos",
        "labelKey": "manage_medicine",
        "icon": "icon-medkit icon-2x",
        "path": "/sghc/pharmacist/manage_medicine"
      },
      {
        "type": "link",
        "route": "pharmacist/manage_prescription",
        "label": "Dispensar medicamentos",
        "labelKey": "provide_medication",
        "icon": "icon-stethoscope icon-2x",
        "path": "/sghc/pharmacist/manage_prescription"
      },
      {
        "type": "link",
        "route": "pharmacist/manage_profile",
        "label": "Meu perfil",
        "labelKey": "profile",
        "icon": "icon-lock icon-2x",
        "path": "/sghc/pharmacist/manage_profile"
      }
    ]
  }
];

export const BAYANNO_SGHC_ROLES = BAYANNO_SGHC_NAV.map((n) => n.role);
