/** Gerado por npm run sync:bayanno-catalog — não editar manualmente. */

export type BayannoScreenKind = 'operational' | 'dashboard' | 'layout';

export type BayannoScreenTab = { id: string; label: string; labelKey: string };
export type BayannoScreenColumn = { label: string; labelKey: string };
export type BayannoScreenTable = { className: string; columns: BayannoScreenColumn[] };

export type BayannoScreen = {
  id: string;
  route: string;
  role: string;
  action: string;
  file: string;
  title: string;
  kind: BayannoScreenKind;
  hasBox: boolean;
  tabs: BayannoScreenTab[];
  tables: BayannoScreenTable[];
  phraseKeys: string[];
  icons: string[];
  moduleLink: string | null;
  path: string;
};

export const BAYANNO_SCREEN_COUNT = 73;

export const BAYANNO_SCREENS: BayannoScreen[] = [
  {
    "id": "accountant-dashboard",
    "route": "accountant/dashboard",
    "role": "accountant",
    "action": "dashboard",
    "file": "accountant/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "invoice / take_payment",
      "view_payment",
      "calendar_schedule",
      "noticeboard"
    ],
    "icons": [
      "icon-tint",
      "icon-money",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/accountant/dashboard"
  },
  {
    "id": "accountant-manage_invoice",
    "route": "accountant/manage_invoice",
    "role": "accountant",
    "action": "manage_invoice",
    "file": "accountant/manage_invoice.php",
    "title": "Faturas",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Invoice",
        "labelKey": "edit_invoice"
      },
      {
        "id": "list",
        "label": "Invoice List",
        "labelKey": "invoice_list"
      },
      {
        "id": "add",
        "label": "Add Invoice",
        "labelKey": "add_invoice"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Title",
            "labelKey": "title"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Status",
            "labelKey": "status"
          },
          {
            "label": "Ações",
            "labelKey": "option"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_invoice",
      "invoice_list",
      "add_invoice",
      "patient",
      "title",
      "amount",
      "description",
      "add_description",
      "status",
      "paid",
      "unpaid",
      "invoice_id",
      "date",
      "option",
      "take_cash_payment",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/financeiro",
    "path": "/sghc/accountant/manage_invoice"
  },
  {
    "id": "accountant-manage_profile",
    "route": "accountant/manage_profile",
    "role": "accountant",
    "action": "manage_profile",
    "file": "accountant/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/accountant/manage_profile"
  },
  {
    "id": "accountant-payment_history",
    "route": "accountant/payment_history",
    "role": "accountant",
    "action": "payment_history",
    "file": "accountant/payment_history.php",
    "title": "Histórico de pagamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Pagamentos",
        "labelKey": "view_payment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Time",
            "labelKey": "time"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Payment Type",
            "labelKey": "payment_type"
          },
          {
            "label": "Transaction Id",
            "labelKey": "transaction_id"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Contadores",
            "labelKey": "accountant"
          },
          {
            "label": "Method",
            "labelKey": "method"
          },
          {
            "label": "Description",
            "labelKey": "description"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_payment",
      "time",
      "amount",
      "payment_type",
      "transaction_id",
      "invoice_id",
      "accountant",
      "method",
      "description"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/financeiro",
    "path": "/sghc/accountant/payment_history"
  },
  {
    "id": "accountant-take_cash_payment",
    "route": "accountant/take_cash_payment",
    "role": "accountant",
    "action": "take_cash_payment",
    "file": "accountant/take_cash_payment.php",
    "title": "Receber pagamento",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "add",
        "label": "Receber pagamento",
        "labelKey": "take_cash_payment"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "take_cash_payment",
      "patient",
      "title",
      "amount",
      "description",
      "add_description",
      "status",
      "paid",
      "unpaid",
      "add_invoice"
    ],
    "icons": [
      "icon-plus"
    ],
    "moduleLink": "/financeiro",
    "path": "/sghc/accountant/take_cash_payment"
  },
  {
    "id": "accountant-view_invoice",
    "route": "accountant/view_invoice",
    "role": "accountant",
    "action": "view_invoice",
    "file": "accountant/view_invoice.php",
    "title": "Faturas",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Invoice List",
        "labelKey": "invoice_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Contadores",
            "labelKey": "accountant"
          },
          {
            "label": "Title",
            "labelKey": "title"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Creation Timestamp",
            "labelKey": "creation_timestamp"
          },
          {
            "label": "Status",
            "labelKey": "status"
          },
          {
            "label": "Ações",
            "labelKey": "option"
          }
        ]
      }
    ],
    "phraseKeys": [
      "invoice_list",
      "invoice_id",
      "amount",
      "accountant",
      "title",
      "description",
      "creation_timestamp",
      "status",
      "option"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/financeiro",
    "path": "/sghc/accountant/view_invoice"
  },
  {
    "id": "accountant-view_payment",
    "route": "accountant/view_payment",
    "role": "accountant",
    "action": "view_payment",
    "file": "accountant/view_payment.php",
    "title": "Pagamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Pagamentos",
        "labelKey": "view_payment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Time",
            "labelKey": "time"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Payment Type",
            "labelKey": "payment_type"
          },
          {
            "label": "Transaction Id",
            "labelKey": "transaction_id"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Method",
            "labelKey": "method"
          },
          {
            "label": "Description",
            "labelKey": "description"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_payment",
      "time",
      "amount",
      "payment_type",
      "transaction_id",
      "invoice_id",
      "patient",
      "method",
      "description"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/financeiro",
    "path": "/sghc/accountant/view_payment"
  },
  {
    "id": "admin-backup_restore",
    "route": "admin/backup_restore",
    "role": "admin",
    "action": "backup_restore",
    "file": "admin/backup_restore.php",
    "title": "Backup e restauração",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "backup",
        "label": "Backup",
        "labelKey": "backup"
      },
      {
        "id": "restore",
        "label": "Restore",
        "labelKey": "restore"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "backup",
      "restore",
      "upload_&_restore_from_backup"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify",
      "icon-download-alt",
      "icon-trash"
    ],
    "moduleLink": "/configuracoes",
    "path": "/sghc/admin/backup_restore"
  },
  {
    "id": "admin-dashboard",
    "route": "admin/dashboard",
    "role": "admin",
    "action": "dashboard",
    "file": "admin/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "doctor",
      "patient",
      "nurse",
      "pharmacist",
      "laboratorist",
      "accountant",
      "appointment",
      "payment",
      "blood_bank",
      "medicine",
      "operation_report",
      "birth_report",
      "death_report",
      "bed_allotment",
      "noticeboard",
      "settings",
      "language",
      "backup",
      "calendar_schedule"
    ],
    "icons": [
      "icon-user-md",
      "icon-user",
      "icon-plus-sign-alt",
      "icon-medkit",
      "icon-beaker",
      "icon-money",
      "icon-exchange",
      "icon-credit-card",
      "icon-tint",
      "icon-medkit",
      "icon-reorder",
      "icon-github-alt",
      "icon-minus-sign",
      "icon-hdd",
      "icon-columns",
      "icon-h-sign",
      "icon-globe",
      "icon-download-alt",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/admin/dashboard"
  },
  {
    "id": "admin-manage_accountant",
    "route": "admin/manage_accountant",
    "role": "admin",
    "action": "manage_accountant",
    "file": "admin/manage_accountant.php",
    "title": "Manage Accountant",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Accountant",
        "labelKey": "edit_accountant"
      },
      {
        "id": "list",
        "label": "Accountant List",
        "labelKey": "accountant_list"
      },
      {
        "id": "add",
        "label": "Add Accountant",
        "labelKey": "add_accountant"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Accountant Name",
            "labelKey": "accountant_name"
          },
          {
            "label": "Email",
            "labelKey": "email"
          },
          {
            "label": "Address",
            "labelKey": "address"
          },
          {
            "label": "Phone",
            "labelKey": "phone"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_accountant",
      "accountant_list",
      "add_accountant",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "accountant_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/usuarios",
    "path": "/sghc/admin/manage_accountant"
  },
  {
    "id": "admin-manage_department",
    "route": "admin/manage_department",
    "role": "admin",
    "action": "manage_department",
    "file": "admin/manage_department.php",
    "title": "Manage Department",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Department",
        "labelKey": "edit_department"
      },
      {
        "id": "list",
        "label": "Department List",
        "labelKey": "department_list"
      },
      {
        "id": "add",
        "label": "Add Department",
        "labelKey": "add_department"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Department Name",
            "labelKey": "department_name"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_department",
      "department_list",
      "add_department",
      "department_name",
      "department_description",
      "description",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/configuracoes/cadastros",
    "path": "/sghc/admin/manage_department"
  },
  {
    "id": "admin-manage_doctor",
    "route": "admin/manage_doctor",
    "role": "admin",
    "action": "manage_doctor",
    "file": "admin/manage_doctor.php",
    "title": "Manage Doctor",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Doctor",
        "labelKey": "edit_doctor"
      },
      {
        "id": "list",
        "label": "Doctor List",
        "labelKey": "doctor_list"
      },
      {
        "id": "add",
        "label": "Add Doctor",
        "labelKey": "add_doctor"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Doctor Name",
            "labelKey": "doctor_name"
          },
          {
            "label": "Departamentos",
            "labelKey": "department"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_doctor",
      "doctor_list",
      "add_doctor",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "department",
      "profile",
      "doctor_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/usuarios",
    "path": "/sghc/admin/manage_doctor"
  },
  {
    "id": "admin-manage_laboratorist",
    "route": "admin/manage_laboratorist",
    "role": "admin",
    "action": "manage_laboratorist",
    "file": "admin/manage_laboratorist.php",
    "title": "Manage Laboratorist",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Laboratorist",
        "labelKey": "edit_laboratorist"
      },
      {
        "id": "list",
        "label": "Laboratorist List",
        "labelKey": "laboratorist_list"
      },
      {
        "id": "add",
        "label": "Add Laboratorist",
        "labelKey": "add_laboratorist"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Laboratorist Name",
            "labelKey": "laboratorist_name"
          },
          {
            "label": "Email",
            "labelKey": "email"
          },
          {
            "label": "Address",
            "labelKey": "address"
          },
          {
            "label": "Phone",
            "labelKey": "phone"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_laboratorist",
      "laboratorist_list",
      "add_laboratorist",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "laboratorist_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/usuarios",
    "path": "/sghc/admin/manage_laboratorist"
  },
  {
    "id": "admin-manage_language",
    "route": "admin/manage_language",
    "role": "admin",
    "action": "manage_language",
    "file": "admin/manage_language.php",
    "title": "Idiomas",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Phrase List",
        "labelKey": "phrase_list"
      },
      {
        "id": "add",
        "label": "Add Phrase",
        "labelKey": "add_phrase"
      },
      {
        "id": "add_lang",
        "label": "Add Language",
        "labelKey": "add_language"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "phrase_list",
      "add_phrase",
      "add_language",
      "phrase",
      "delete_language",
      "update_phrase",
      "language"
    ],
    "icons": [
      "icon-align-justify",
      "icon-plus",
      "icon-plus",
      "icon-trash",
      "icon-angle-right"
    ],
    "moduleLink": "/configuracoes",
    "path": "/sghc/admin/manage_language"
  },
  {
    "id": "admin-manage_noticeboard",
    "route": "admin/manage_noticeboard",
    "role": "admin",
    "action": "manage_noticeboard",
    "file": "admin/manage_noticeboard.php",
    "title": "Mural de avisos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Noticeboard",
        "labelKey": "edit_noticeboard"
      },
      {
        "id": "list",
        "label": "Noticeboard List",
        "labelKey": "noticeboard_list"
      },
      {
        "id": "add",
        "label": "Add Noticeboard",
        "labelKey": "add_noticeboard"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Title",
            "labelKey": "title"
          },
          {
            "label": "Notice",
            "labelKey": "notice"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_noticeboard",
      "noticeboard_list",
      "add_noticeboard",
      "title",
      "notice",
      "date",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/configuracoes",
    "path": "/sghc/admin/manage_noticeboard"
  },
  {
    "id": "admin-manage_nurse",
    "route": "admin/manage_nurse",
    "role": "admin",
    "action": "manage_nurse",
    "file": "admin/manage_nurse.php",
    "title": "Manage Nurse",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Nurse",
        "labelKey": "edit_nurse"
      },
      {
        "id": "list",
        "label": "Nurse List",
        "labelKey": "nurse_list"
      },
      {
        "id": "add",
        "label": "Add Nurse",
        "labelKey": "add_nurse"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Nurse Name",
            "labelKey": "nurse_name"
          },
          {
            "label": "Email",
            "labelKey": "email"
          },
          {
            "label": "Address",
            "labelKey": "address"
          },
          {
            "label": "Phone",
            "labelKey": "phone"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_nurse",
      "nurse_list",
      "add_nurse",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "nurse_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/usuarios",
    "path": "/sghc/admin/manage_nurse"
  },
  {
    "id": "admin-manage_patient",
    "route": "admin/manage_patient",
    "role": "admin",
    "action": "manage_patient",
    "file": "admin/manage_patient.php",
    "title": "Manage Patient",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Patient",
        "labelKey": "edit_patient"
      },
      {
        "id": "list",
        "label": "Patient List",
        "labelKey": "patient_list"
      },
      {
        "id": "add",
        "label": "Add Patient",
        "labelKey": "add_patient"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Patient Name",
            "labelKey": "patient_name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Birth Date",
            "labelKey": "birth_date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_patient",
      "patient_list",
      "add_patient",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "sex",
      "male",
      "female",
      "birth_date",
      "age",
      "blood_group",
      "patient_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/recepcao/pacientes",
    "path": "/sghc/admin/manage_patient"
  },
  {
    "id": "admin-manage_pharmacist",
    "route": "admin/manage_pharmacist",
    "role": "admin",
    "action": "manage_pharmacist",
    "file": "admin/manage_pharmacist.php",
    "title": "Manage Pharmacist",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Pharmacist",
        "labelKey": "edit_pharmacist"
      },
      {
        "id": "list",
        "label": "Pharmacist List",
        "labelKey": "pharmacist_list"
      },
      {
        "id": "add",
        "label": "Add Pharmacist",
        "labelKey": "add_pharmacist"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Pharmacist Name",
            "labelKey": "pharmacist_name"
          },
          {
            "label": "Email",
            "labelKey": "email"
          },
          {
            "label": "Address",
            "labelKey": "address"
          },
          {
            "label": "Phone",
            "labelKey": "phone"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_pharmacist",
      "pharmacist_list",
      "add_pharmacist",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "pharmacist_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/usuarios",
    "path": "/sghc/admin/manage_pharmacist"
  },
  {
    "id": "admin-manage_profile",
    "route": "admin/manage_profile",
    "role": "admin",
    "action": "manage_profile",
    "file": "admin/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/admin/manage_profile"
  },
  {
    "id": "admin-system_settings",
    "route": "admin/system_settings",
    "role": "admin",
    "action": "system_settings",
    "file": "admin/system_settings.php",
    "title": "Configurações do sistema",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Configurações do sistema",
        "labelKey": "system_settings"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "system_settings",
      "save"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/configuracoes",
    "path": "/sghc/admin/system_settings"
  },
  {
    "id": "admin-view_appointment",
    "route": "admin/view_appointment",
    "role": "admin",
    "action": "view_appointment",
    "file": "admin/view_appointment.php",
    "title": "Agendamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Agendamentos",
        "labelKey": "view_appointment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Time",
            "labelKey": "time"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_appointment",
      "time",
      "doctor",
      "patient"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/recepcao/agendamentos",
    "path": "/sghc/admin/view_appointment"
  },
  {
    "id": "admin-view_bed_status",
    "route": "admin/view_bed_status",
    "role": "admin",
    "action": "view_bed_status",
    "file": "admin/view_bed_status.php",
    "title": "Status de leitos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Alocação de leitos",
        "labelKey": "bed_allotment"
      },
      {
        "id": "list_blood_bank",
        "label": "Bed List",
        "labelKey": "bed_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Bed Id",
            "labelKey": "bed_id"
          },
          {
            "label": "Bed Type",
            "labelKey": "bed_type"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Allotment Time",
            "labelKey": "allotment_time"
          },
          {
            "label": "Discharge Time",
            "labelKey": "discharge_time"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Bed Number",
            "labelKey": "bed_number"
          },
          {
            "label": "Type",
            "labelKey": "type"
          }
        ]
      }
    ],
    "phraseKeys": [
      "bed_allotment",
      "bed_list",
      "bed_id",
      "bed_type",
      "patient",
      "allotment_time",
      "discharge_time",
      "bed_number",
      "type"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify"
    ],
    "moduleLink": "/internacao/leitos",
    "path": "/sghc/admin/view_bed_status"
  },
  {
    "id": "admin-view_blood_bank",
    "route": "admin/view_blood_bank",
    "role": "admin",
    "action": "view_blood_bank",
    "file": "admin/view_blood_bank.php",
    "title": "Banco de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Blood Donor List",
        "labelKey": "blood_donor_list"
      },
      {
        "id": "list_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "blood_bank"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Last Donation Date",
            "labelKey": "last_donation_date"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Status",
            "labelKey": "status"
          }
        ]
      }
    ],
    "phraseKeys": [
      "blood_donor_list",
      "blood_bank",
      "name",
      "age",
      "sex",
      "blood_group",
      "last_donation_date",
      "status"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/admin/view_blood_bank"
  },
  {
    "id": "admin-view_log",
    "route": "admin/view_log",
    "role": "admin",
    "action": "view_log",
    "file": "admin/view_log.php",
    "title": "Logs do sistema",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Logs do sistema",
        "labelKey": "view_log"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Type",
            "labelKey": "type"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "User",
            "labelKey": "user"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Ip",
            "labelKey": "ip"
          },
          {
            "label": "Location",
            "labelKey": "location"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_log",
      "type",
      "date",
      "user",
      "name",
      "description",
      "ip",
      "location"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/auditoria",
    "path": "/sghc/admin/view_log"
  },
  {
    "id": "admin-view_medicine",
    "route": "admin/view_medicine",
    "role": "admin",
    "action": "view_medicine",
    "file": "admin/view_medicine.php",
    "title": "Medicamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Medicamentos",
        "labelKey": "view_medicine"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Category",
            "labelKey": "category"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Price",
            "labelKey": "price"
          },
          {
            "label": "Manufacturing Company",
            "labelKey": "manufacturing_company"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_medicine",
      "name",
      "category",
      "description",
      "price",
      "manufacturing_company"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/farmacia",
    "path": "/sghc/admin/view_medicine"
  },
  {
    "id": "admin-view_payment",
    "route": "admin/view_payment",
    "role": "admin",
    "action": "view_payment",
    "file": "admin/view_payment.php",
    "title": "Pagamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Pagamentos",
        "labelKey": "view_payment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Time",
            "labelKey": "time"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Payment Type",
            "labelKey": "payment_type"
          },
          {
            "label": "Transaction Id",
            "labelKey": "transaction_id"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Method",
            "labelKey": "method"
          },
          {
            "label": "Description",
            "labelKey": "description"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_payment",
      "time",
      "amount",
      "payment_type",
      "transaction_id",
      "invoice_id",
      "patient",
      "method",
      "description"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/financeiro",
    "path": "/sghc/admin/view_payment"
  },
  {
    "id": "admin-view_report",
    "route": "admin/view_report",
    "role": "admin",
    "action": "view_report",
    "file": "admin/view_report.php",
    "title": "View Report",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "View Report",
        "labelKey": "view_report"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_report",
      "description",
      "date",
      "patient",
      "doctor"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/relatorios",
    "path": "/sghc/admin/view_report"
  },
  {
    "id": "doctor-dashboard",
    "route": "doctor/dashboard",
    "role": "doctor",
    "action": "dashboard",
    "file": "doctor/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "patient",
      "appointment",
      "prescription",
      "bed_allotment",
      "blood_bank",
      "manage_report",
      "calendar_schedule",
      "noticeboard"
    ],
    "icons": [
      "icon-user",
      "icon-exchange",
      "icon-stethoscope",
      "icon-hdd",
      "icon-tint",
      "icon-hospital",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/doctor/dashboard"
  },
  {
    "id": "doctor-manage_appointment",
    "route": "doctor/manage_appointment",
    "role": "doctor",
    "action": "manage_appointment",
    "file": "doctor/manage_appointment.php",
    "title": "Agendamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Appointment",
        "labelKey": "edit_appointment"
      },
      {
        "id": "list",
        "label": "Appointment List",
        "labelKey": "appointment_list"
      },
      {
        "id": "add",
        "label": "Add Appointment",
        "labelKey": "add_appointment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_appointment",
      "appointment_list",
      "add_appointment",
      "doctor",
      "patient",
      "date",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/recepcao/agendamentos",
    "path": "/sghc/doctor/manage_appointment"
  },
  {
    "id": "doctor-manage_bed_allotment",
    "route": "doctor/manage_bed_allotment",
    "role": "doctor",
    "action": "manage_bed_allotment",
    "file": "doctor/manage_bed_allotment.php",
    "title": "Alocação de leitos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Bed Allotment",
        "labelKey": "edit_bed_allotment"
      },
      {
        "id": "list",
        "label": "Bed Allotment List",
        "labelKey": "bed_allotment_list"
      },
      {
        "id": "add",
        "label": "Add Bed Allotment",
        "labelKey": "add_bed_allotment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Bed Number",
            "labelKey": "bed_number"
          },
          {
            "label": "Bed Type",
            "labelKey": "bed_type"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Allotment Date Time",
            "labelKey": "allotment_date_time"
          },
          {
            "label": "Discharge Date Time",
            "labelKey": "discharge_date_time"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_bed_allotment",
      "bed_allotment_list",
      "add_bed_allotment",
      "bed_number",
      "patient",
      "allotment_time",
      "discharge_time",
      "bed_type",
      "allotment_date_time",
      "discharge_date_time",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/internacao/leitos",
    "path": "/sghc/doctor/manage_bed_allotment"
  },
  {
    "id": "doctor-manage_patient",
    "route": "doctor/manage_patient",
    "role": "doctor",
    "action": "manage_patient",
    "file": "doctor/manage_patient.php",
    "title": "Manage Patient",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Patient",
        "labelKey": "edit_patient"
      },
      {
        "id": "list",
        "label": "Patient List",
        "labelKey": "patient_list"
      },
      {
        "id": "add",
        "label": "Add Patient",
        "labelKey": "add_patient"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Patient Name",
            "labelKey": "patient_name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Birth Date",
            "labelKey": "birth_date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_patient",
      "patient_list",
      "add_patient",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "sex",
      "male",
      "female",
      "birth_date",
      "age",
      "blood_group",
      "patient_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/recepcao/pacientes",
    "path": "/sghc/doctor/manage_patient"
  },
  {
    "id": "doctor-manage_prescription",
    "route": "doctor/manage_prescription",
    "role": "doctor",
    "action": "manage_prescription",
    "file": "doctor/manage_prescription.php",
    "title": "Prescrições",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Prescription",
        "labelKey": "edit_prescription"
      },
      {
        "id": "list",
        "label": "Prescription List",
        "labelKey": "prescription_list"
      },
      {
        "id": "add",
        "label": "Add Prescription",
        "labelKey": "add_prescription"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_prescription",
      "prescription_list",
      "add_prescription",
      "doctor",
      "patient",
      "case_history",
      "add_description",
      "medication",
      "medication_from_pharmacist",
      "description",
      "date",
      "diagnosis_report",
      "report_type",
      "document_type",
      "download",
      "laboratorist",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-download-alt",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/pep/prescricao",
    "path": "/sghc/doctor/manage_prescription"
  },
  {
    "id": "doctor-manage_profile",
    "route": "doctor/manage_profile",
    "role": "doctor",
    "action": "manage_profile",
    "file": "doctor/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/doctor/manage_profile"
  },
  {
    "id": "doctor-manage_report",
    "route": "doctor/manage_report",
    "role": "doctor",
    "action": "manage_report",
    "file": "doctor/manage_report.php",
    "title": "Relatórios clínicos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "operation",
        "label": "Operation",
        "labelKey": "operation"
      },
      {
        "id": "birth",
        "label": "Birth",
        "labelKey": "birth"
      },
      {
        "id": "death",
        "label": "Death",
        "labelKey": "death"
      },
      {
        "id": "other",
        "label": "Other",
        "labelKey": "other"
      },
      {
        "id": "add",
        "label": "Add Report",
        "labelKey": "add_report"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "operation",
      "birth",
      "death",
      "other",
      "add_report",
      "description",
      "date",
      "patient",
      "doctor",
      "options",
      "delete",
      "type"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify",
      "icon-align-justify",
      "icon-align-justify",
      "icon-plus",
      "icon-trash",
      "icon-trash",
      "icon-trash",
      "icon-trash"
    ],
    "moduleLink": "/relatorios",
    "path": "/sghc/doctor/manage_report"
  },
  {
    "id": "doctor-view_blood_bank",
    "route": "doctor/view_blood_bank",
    "role": "doctor",
    "action": "view_blood_bank",
    "file": "doctor/view_blood_bank.php",
    "title": "Banco de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Blood Donor List",
        "labelKey": "blood_donor_list"
      },
      {
        "id": "list_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "blood_bank"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Last Donation Date",
            "labelKey": "last_donation_date"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Status",
            "labelKey": "status"
          }
        ]
      }
    ],
    "phraseKeys": [
      "blood_donor_list",
      "blood_bank",
      "name",
      "age",
      "sex",
      "blood_group",
      "last_donation_date",
      "status"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/doctor/view_blood_bank"
  },
  {
    "id": "footer",
    "route": "footer",
    "role": "root",
    "action": "footer",
    "file": "footer.php",
    "title": "Footer",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [],
    "icons": [],
    "moduleLink": null,
    "path": "/sghc/footer"
  },
  {
    "id": "four_zero_four",
    "route": "four_zero_four",
    "role": "root",
    "action": "four_zero_four",
    "file": "four_zero_four.php",
    "title": "Four Zero Four",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [],
    "icons": [
      "icon-arrow-left"
    ],
    "moduleLink": null,
    "path": "/sghc/four_zero_four"
  },
  {
    "id": "header",
    "route": "header",
    "role": "root",
    "action": "header",
    "file": "header.php",
    "title": "Header",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "account",
      "profile",
      "logout",
      "select_language",
      "panel"
    ],
    "icons": [
      "icon-th-list",
      "icon-align-justify",
      "icon-user",
      "icon-off",
      "icon-ok",
      "icon-user"
    ],
    "moduleLink": null,
    "path": "/sghc/header"
  },
  {
    "id": "includes",
    "route": "includes",
    "role": "root",
    "action": "includes",
    "file": "includes.php",
    "title": "Includes",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [],
    "icons": [],
    "moduleLink": null,
    "path": "/sghc/includes"
  },
  {
    "id": "index",
    "route": "index",
    "role": "root",
    "action": "index",
    "file": "index.php",
    "title": "Index",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [],
    "icons": [],
    "moduleLink": null,
    "path": "/sghc/index"
  },
  {
    "id": "install-index",
    "route": "install/index",
    "role": "install",
    "action": "index",
    "file": "install/index.php",
    "title": "Index",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [],
    "icons": [],
    "moduleLink": null,
    "path": "/sghc/install/index"
  },
  {
    "id": "laboratorist-dashboard",
    "route": "laboratorist/dashboard",
    "role": "laboratorist",
    "action": "dashboard",
    "file": "laboratorist/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "add_diagnosis_report",
      "manage_blood_bank",
      "manage_blood_donor",
      "calendar_schedule",
      "noticeboard"
    ],
    "icons": [
      "icon-stethoscope",
      "icon-tint",
      "icon-user",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/laboratorist/dashboard"
  },
  {
    "id": "laboratorist-manage_blood_bank",
    "route": "laboratorist/manage_blood_bank",
    "role": "laboratorist",
    "action": "manage_blood_bank",
    "file": "laboratorist/manage_blood_bank.php",
    "title": "Banco de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Blood Bank",
        "labelKey": "edit_blood_bank"
      },
      {
        "id": "list",
        "label": "Blood Bank List",
        "labelKey": "blood_bank_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Status",
            "labelKey": "status"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_blood_bank",
      "blood_bank_list",
      "blood_group",
      "status",
      "options",
      "edit"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-wrench"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/laboratorist/manage_blood_bank"
  },
  {
    "id": "laboratorist-manage_blood_donor",
    "route": "laboratorist/manage_blood_donor",
    "role": "laboratorist",
    "action": "manage_blood_donor",
    "file": "laboratorist/manage_blood_donor.php",
    "title": "Doadores de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Blood Donor",
        "labelKey": "edit_blood_donor"
      },
      {
        "id": "list",
        "label": "Blood Donor List",
        "labelKey": "blood_donor_list"
      },
      {
        "id": "add",
        "label": "Add Blood Donor",
        "labelKey": "add_blood_donor"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Last Donation Date",
            "labelKey": "last_donation_date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_blood_donor",
      "blood_donor_list",
      "add_blood_donor",
      "name",
      "email",
      "address",
      "phone",
      "sex",
      "male",
      "female",
      "age",
      "blood_group",
      "last_donation_date",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/laboratorist/manage_blood_donor"
  },
  {
    "id": "laboratorist-manage_prescription",
    "route": "laboratorist/manage_prescription",
    "role": "laboratorist",
    "action": "manage_prescription",
    "file": "laboratorist/manage_prescription.php",
    "title": "Prescrições",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Prescription",
        "labelKey": "edit_prescription"
      },
      {
        "id": "list",
        "label": "Prescription List",
        "labelKey": "prescription_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Report Status",
            "labelKey": "report_status"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_prescription",
      "prescription_list",
      "doctor",
      "patient",
      "case_history",
      "medication",
      "medication_from_pharmacist",
      "description",
      "date",
      "diagnosis_report",
      "report_type",
      "document_type",
      "download",
      "laboratorist",
      "option",
      "delete",
      "add_diagnosis_report",
      "image",
      "doc",
      "pdf",
      "excel",
      "other",
      "upload_document",
      "report_status",
      "options",
      "edit",
      "add_diagnostic_report"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-download-alt",
      "icon-trash",
      "icon-wrench"
    ],
    "moduleLink": "/laboratorio",
    "path": "/sghc/laboratorist/manage_prescription"
  },
  {
    "id": "laboratorist-manage_profile",
    "route": "laboratorist/manage_profile",
    "role": "laboratorist",
    "action": "manage_profile",
    "file": "laboratorist/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/laboratorist/manage_profile"
  },
  {
    "id": "laboratorist-view_blood_bank",
    "route": "laboratorist/view_blood_bank",
    "role": "laboratorist",
    "action": "view_blood_bank",
    "file": "laboratorist/view_blood_bank.php",
    "title": "Banco de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Blood Donor List",
        "labelKey": "blood_donor_list"
      },
      {
        "id": "list_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "blood_bank"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Last Donation Date",
            "labelKey": "last_donation_date"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Status",
            "labelKey": "status"
          }
        ]
      }
    ],
    "phraseKeys": [
      "blood_donor_list",
      "blood_bank",
      "name",
      "age",
      "sex",
      "blood_group",
      "last_donation_date",
      "status"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/laboratorist/view_blood_bank"
  },
  {
    "id": "login",
    "route": "login",
    "role": "root",
    "action": "login",
    "file": "login.php",
    "title": "Login",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "login",
      "account_type",
      "admin",
      "doctor",
      "patient",
      "nurse",
      "pharmacist",
      "laboratorist",
      "accountant",
      "email",
      "password",
      "forgot_password?",
      "reset_password",
      "reset"
    ],
    "icons": [
      "icon-ok",
      "icon-swapright",
      "icon-white",
      "icon-envelope",
      "icon-key"
    ],
    "moduleLink": "/login",
    "path": "/sghc/login"
  },
  {
    "id": "nurse-dashboard",
    "route": "nurse/dashboard",
    "role": "nurse",
    "action": "dashboard",
    "file": "nurse/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "patient",
      "bed_allotment",
      "blood_bank",
      "report",
      "calendar_schedule",
      "noticeboard"
    ],
    "icons": [
      "icon-user",
      "icon-hdd",
      "icon-tint",
      "icon-hospital",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/nurse/dashboard"
  },
  {
    "id": "nurse-manage_bed_allotment",
    "route": "nurse/manage_bed_allotment",
    "role": "nurse",
    "action": "manage_bed_allotment",
    "file": "nurse/manage_bed_allotment.php",
    "title": "Alocação de leitos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Bed Allotment",
        "labelKey": "edit_bed_allotment"
      },
      {
        "id": "list",
        "label": "Bed Allotment List",
        "labelKey": "bed_allotment_list"
      },
      {
        "id": "add",
        "label": "Add Bed Allotment",
        "labelKey": "add_bed_allotment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Bed Number",
            "labelKey": "bed_number"
          },
          {
            "label": "Bed Type",
            "labelKey": "bed_type"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Allotment Date Time",
            "labelKey": "allotment_date_time"
          },
          {
            "label": "Discharge Date Time",
            "labelKey": "discharge_date_time"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_bed_allotment",
      "bed_allotment_list",
      "add_bed_allotment",
      "bed_number",
      "patient",
      "allotment_time",
      "discharge_time",
      "bed_type",
      "allotment_date_time",
      "discharge_date_time",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/internacao/leitos",
    "path": "/sghc/nurse/manage_bed_allotment"
  },
  {
    "id": "nurse-manage_bed",
    "route": "nurse/manage_bed",
    "role": "nurse",
    "action": "manage_bed",
    "file": "nurse/manage_bed.php",
    "title": "Leitos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Bed",
        "labelKey": "edit_bed"
      },
      {
        "id": "list",
        "label": "Bed List",
        "labelKey": "bed_list"
      },
      {
        "id": "add",
        "label": "Add Bed",
        "labelKey": "add_bed"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Bed Number",
            "labelKey": "bed_number"
          },
          {
            "label": "Type",
            "labelKey": "type"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_bed",
      "bed_list",
      "add_bed",
      "bed_number",
      "type",
      "ward",
      "cabin",
      "icu",
      "other",
      "description",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/internacao/leitos",
    "path": "/sghc/nurse/manage_bed"
  },
  {
    "id": "nurse-manage_blood_bank",
    "route": "nurse/manage_blood_bank",
    "role": "nurse",
    "action": "manage_blood_bank",
    "file": "nurse/manage_blood_bank.php",
    "title": "Banco de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Blood Bank",
        "labelKey": "edit_blood_bank"
      },
      {
        "id": "list",
        "label": "Blood Bank List",
        "labelKey": "blood_bank_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Status",
            "labelKey": "status"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_blood_bank",
      "blood_bank_list",
      "blood_group",
      "status",
      "options",
      "edit"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-wrench"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/nurse/manage_blood_bank"
  },
  {
    "id": "nurse-manage_blood_donor",
    "route": "nurse/manage_blood_donor",
    "role": "nurse",
    "action": "manage_blood_donor",
    "file": "nurse/manage_blood_donor.php",
    "title": "Doadores de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Blood Donor",
        "labelKey": "edit_blood_donor"
      },
      {
        "id": "list",
        "label": "Blood Donor List",
        "labelKey": "blood_donor_list"
      },
      {
        "id": "add",
        "label": "Add Blood Donor",
        "labelKey": "add_blood_donor"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Last Donation Date",
            "labelKey": "last_donation_date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_blood_donor",
      "blood_donor_list",
      "add_blood_donor",
      "name",
      "email",
      "address",
      "phone",
      "sex",
      "male",
      "female",
      "age",
      "blood_group",
      "last_donation_date",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/hemoterapia",
    "path": "/sghc/nurse/manage_blood_donor"
  },
  {
    "id": "nurse-manage_patient",
    "route": "nurse/manage_patient",
    "role": "nurse",
    "action": "manage_patient",
    "file": "nurse/manage_patient.php",
    "title": "Manage Patient",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Patient",
        "labelKey": "edit_patient"
      },
      {
        "id": "list",
        "label": "Patient List",
        "labelKey": "patient_list"
      },
      {
        "id": "add",
        "label": "Add Patient",
        "labelKey": "add_patient"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Patient Name",
            "labelKey": "patient_name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Birth Date",
            "labelKey": "birth_date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_patient",
      "patient_list",
      "add_patient",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "sex",
      "male",
      "female",
      "birth_date",
      "age",
      "blood_group",
      "patient_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/recepcao/pacientes",
    "path": "/sghc/nurse/manage_patient"
  },
  {
    "id": "nurse-manage_profile",
    "route": "nurse/manage_profile",
    "role": "nurse",
    "action": "manage_profile",
    "file": "nurse/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/nurse/manage_profile"
  },
  {
    "id": "nurse-manage_report",
    "route": "nurse/manage_report",
    "role": "nurse",
    "action": "manage_report",
    "file": "nurse/manage_report.php",
    "title": "Relatórios clínicos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "operation",
        "label": "Operation",
        "labelKey": "operation"
      },
      {
        "id": "birth",
        "label": "Birth",
        "labelKey": "birth"
      },
      {
        "id": "death",
        "label": "Death",
        "labelKey": "death"
      },
      {
        "id": "other",
        "label": "Other",
        "labelKey": "other"
      },
      {
        "id": "add",
        "label": "Add Report",
        "labelKey": "add_report"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "operation",
      "birth",
      "death",
      "other",
      "add_report",
      "description",
      "date",
      "patient",
      "doctor",
      "options",
      "delete",
      "type"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify",
      "icon-align-justify",
      "icon-align-justify",
      "icon-plus",
      "icon-trash",
      "icon-trash",
      "icon-trash",
      "icon-trash"
    ],
    "moduleLink": "/relatorios",
    "path": "/sghc/nurse/manage_report"
  },
  {
    "id": "page_info",
    "route": "page_info",
    "role": "root",
    "action": "page_info",
    "file": "page_info.php",
    "title": "Médicos",
    "kind": "layout",
    "hasBox": false,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "doctor",
      "patient",
      "appointment",
      "nurse"
    ],
    "icons": [
      "icon-info-sign"
    ],
    "moduleLink": null,
    "path": "/sghc/page_info"
  },
  {
    "id": "patient-dashboard",
    "route": "patient/dashboard",
    "role": "patient",
    "action": "dashboard",
    "file": "patient/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "doctor",
      "appointment",
      "prescription",
      "admit_history",
      "blood_bank",
      "view_invoice",
      "calendar_schedule",
      "noticeboard"
    ],
    "icons": [
      "icon-stethoscope",
      "icon-exchange",
      "icon-stethoscope",
      "icon-hdd",
      "icon-tint",
      "icon-credit-card",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/patient/dashboard"
  },
  {
    "id": "patient-manage_patient",
    "route": "patient/manage_patient",
    "role": "patient",
    "action": "manage_patient",
    "file": "patient/manage_patient.php",
    "title": "Manage Patient",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Patient",
        "labelKey": "edit_patient"
      },
      {
        "id": "list",
        "label": "Patient List",
        "labelKey": "patient_list"
      },
      {
        "id": "add",
        "label": "Add Patient",
        "labelKey": "add_patient"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Patient Name",
            "labelKey": "patient_name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Birth Date",
            "labelKey": "birth_date"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_patient",
      "patient_list",
      "add_patient",
      "name",
      "email",
      "password",
      "address",
      "phone",
      "sex",
      "male",
      "female",
      "birth_date",
      "age",
      "blood_group",
      "patient_name",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/patient/manage_patient"
  },
  {
    "id": "patient-manage_profile",
    "route": "patient/manage_profile",
    "role": "patient",
    "action": "manage_profile",
    "file": "patient/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/patient/manage_profile"
  },
  {
    "id": "patient-payment_history",
    "route": "patient/payment_history",
    "role": "patient",
    "action": "payment_history",
    "file": "patient/payment_history.php",
    "title": "Histórico de pagamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Pagamentos",
        "labelKey": "view_payment"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Time",
            "labelKey": "time"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Payment Type",
            "labelKey": "payment_type"
          },
          {
            "label": "Transaction Id",
            "labelKey": "transaction_id"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Method",
            "labelKey": "method"
          },
          {
            "label": "Description",
            "labelKey": "description"
          }
        ]
      }
    ],
    "phraseKeys": [
      "view_payment",
      "time",
      "amount",
      "payment_type",
      "transaction_id",
      "invoice_id",
      "patient",
      "method",
      "description"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/payment_history"
  },
  {
    "id": "patient-view_admit_history",
    "route": "patient/view_admit_history",
    "role": "patient",
    "action": "view_admit_history",
    "file": "patient/view_admit_history.php",
    "title": "View Admit History",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Bed Allotment List",
        "labelKey": "bed_allotment_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Bed Number",
            "labelKey": "bed_number"
          },
          {
            "label": "Bed Type",
            "labelKey": "bed_type"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Allotment Date Time",
            "labelKey": "allotment_date_time"
          },
          {
            "label": "Discharge Date Time",
            "labelKey": "discharge_date_time"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "bed_allotment_list",
      "bed_number",
      "bed_type",
      "patient",
      "allotment_date_time",
      "discharge_date_time",
      "options"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_admit_history"
  },
  {
    "id": "patient-view_appointment",
    "route": "patient/view_appointment",
    "role": "patient",
    "action": "view_appointment",
    "file": "patient/view_appointment.php",
    "title": "Agendamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Appointment List",
        "labelKey": "appointment_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Departamentos",
            "labelKey": "department"
          }
        ]
      }
    ],
    "phraseKeys": [
      "appointment_list",
      "date",
      "doctor",
      "department"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_appointment"
  },
  {
    "id": "patient-view_blood_bank",
    "route": "patient/view_blood_bank",
    "role": "patient",
    "action": "view_blood_bank",
    "file": "patient/view_blood_bank.php",
    "title": "Banco de sangue",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Blood Donor List",
        "labelKey": "blood_donor_list"
      },
      {
        "id": "list_blood_bank",
        "label": "Banco de sangue",
        "labelKey": "blood_bank"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Name",
            "labelKey": "name"
          },
          {
            "label": "Age",
            "labelKey": "age"
          },
          {
            "label": "Sex",
            "labelKey": "sex"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Last Donation Date",
            "labelKey": "last_donation_date"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Blood Group",
            "labelKey": "blood_group"
          },
          {
            "label": "Status",
            "labelKey": "status"
          }
        ]
      }
    ],
    "phraseKeys": [
      "blood_donor_list",
      "blood_bank",
      "name",
      "age",
      "sex",
      "blood_group",
      "last_donation_date",
      "status"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_blood_bank"
  },
  {
    "id": "patient-view_doctor",
    "route": "patient/view_doctor",
    "role": "patient",
    "action": "view_doctor",
    "file": "patient/view_doctor.php",
    "title": "Médicos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Doctor List",
        "labelKey": "doctor_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Doctor Name",
            "labelKey": "doctor_name"
          },
          {
            "label": "Departamentos",
            "labelKey": "department"
          }
        ]
      }
    ],
    "phraseKeys": [
      "doctor_list",
      "doctor_name",
      "department"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_doctor"
  },
  {
    "id": "patient-view_invoice",
    "route": "patient/view_invoice",
    "role": "patient",
    "action": "view_invoice",
    "file": "patient/view_invoice.php",
    "title": "Faturas",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Invoice List",
        "labelKey": "invoice_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Invoice Id",
            "labelKey": "invoice_id"
          },
          {
            "label": "Amount",
            "labelKey": "amount"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Title",
            "labelKey": "title"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Creation Timestamp",
            "labelKey": "creation_timestamp"
          },
          {
            "label": "Status",
            "labelKey": "status"
          },
          {
            "label": "Ações",
            "labelKey": "option"
          }
        ]
      }
    ],
    "phraseKeys": [
      "invoice_list",
      "invoice_id",
      "amount",
      "patient",
      "title",
      "description",
      "creation_timestamp",
      "status",
      "option"
    ],
    "icons": [
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_invoice"
  },
  {
    "id": "patient-view_operation_history",
    "route": "patient/view_operation_history",
    "role": "patient",
    "action": "view_operation_history",
    "file": "patient/view_operation_history.php",
    "title": "View Operation History",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "operation",
        "label": "Operation",
        "labelKey": "operation"
      },
      {
        "id": "birth",
        "label": "Birth",
        "labelKey": "birth"
      },
      {
        "id": "other",
        "label": "Other",
        "labelKey": "other"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      },
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "operation",
      "birth",
      "other",
      "description",
      "date",
      "patient",
      "options"
    ],
    "icons": [
      "icon-align-justify",
      "icon-align-justify",
      "icon-align-justify"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_operation_history"
  },
  {
    "id": "patient-view_prescription",
    "route": "patient/view_prescription",
    "role": "patient",
    "action": "view_prescription",
    "file": "patient/view_prescription.php",
    "title": "Prescrições",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Prescription",
        "labelKey": "edit_prescription"
      },
      {
        "id": "list",
        "label": "Prescription List",
        "labelKey": "prescription_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_prescription",
      "prescription_list",
      "doctor",
      "patient",
      "case_history",
      "medication",
      "medication_from_pharmacist",
      "description",
      "date",
      "diagnosis_report",
      "report_type",
      "document_type",
      "download",
      "laboratorist",
      "options",
      "view_prescription"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-download-alt"
    ],
    "moduleLink": "/portal-paciente",
    "path": "/sghc/patient/view_prescription"
  },
  {
    "id": "pharmacist-dashboard",
    "route": "pharmacist/dashboard",
    "role": "pharmacist",
    "action": "dashboard",
    "file": "pharmacist/dashboard.php",
    "title": "Painel",
    "kind": "dashboard",
    "hasBox": true,
    "tabs": [],
    "tables": [],
    "phraseKeys": [
      "medicine_category",
      "manage_medicine",
      "provide_medication",
      "calendar_schedule",
      "noticeboard"
    ],
    "icons": [
      "icon-edit",
      "icon-medkit",
      "icon-stethoscope",
      "icon-calendar",
      "icon-reorder",
      "icon-tag",
      "icon-2x"
    ],
    "moduleLink": "/",
    "path": "/sghc/pharmacist/dashboard"
  },
  {
    "id": "pharmacist-manage_medicine_category",
    "route": "pharmacist/manage_medicine_category",
    "role": "pharmacist",
    "action": "manage_medicine_category",
    "file": "pharmacist/manage_medicine_category.php",
    "title": "Manage Medicine Category",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Medicine Category",
        "labelKey": "edit_medicine_category"
      },
      {
        "id": "list",
        "label": "Medicine Category List",
        "labelKey": "medicine_category_list"
      },
      {
        "id": "add",
        "label": "Add Medicine Category",
        "labelKey": "add_medicine_category"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Medicine Category Name",
            "labelKey": "medicine_category_name"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_medicine_category",
      "medicine_category_list",
      "add_medicine_category",
      "medicine_category_name",
      "medicine_category_description",
      "description",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/farmacia",
    "path": "/sghc/pharmacist/manage_medicine_category"
  },
  {
    "id": "pharmacist-manage_medicine",
    "route": "pharmacist/manage_medicine",
    "role": "pharmacist",
    "action": "manage_medicine",
    "file": "pharmacist/manage_medicine.php",
    "title": "Medicamentos",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Medicine",
        "labelKey": "edit_medicine"
      },
      {
        "id": "list",
        "label": "Medicine List",
        "labelKey": "medicine_list"
      },
      {
        "id": "add",
        "label": "Add Medicine",
        "labelKey": "add_medicine"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Medicine Name",
            "labelKey": "medicine_name"
          },
          {
            "label": "Medicine Catogory",
            "labelKey": "medicine_catogory"
          },
          {
            "label": "Description",
            "labelKey": "description"
          },
          {
            "label": "Price",
            "labelKey": "price"
          },
          {
            "label": "Manufacturing Company",
            "labelKey": "manufacturing_company"
          },
          {
            "label": "Status",
            "labelKey": "status"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_medicine",
      "medicine_list",
      "add_medicine",
      "name",
      "medicine_category",
      "description",
      "price",
      "manufacturing_company",
      "status",
      "medicine_name",
      "medicine_catogory",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-plus",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/farmacia",
    "path": "/sghc/pharmacist/manage_medicine"
  },
  {
    "id": "pharmacist-manage_prescription",
    "route": "pharmacist/manage_prescription",
    "role": "pharmacist",
    "action": "manage_prescription",
    "file": "pharmacist/manage_prescription.php",
    "title": "Prescrições",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "edit",
        "label": "Edit Prescription",
        "labelKey": "edit_prescription"
      },
      {
        "id": "list",
        "label": "Prescription List",
        "labelKey": "prescription_list"
      }
    ],
    "tables": [
      {
        "className": "dTable responsive",
        "columns": [
          {
            "label": "#",
            "labelKey": "#"
          },
          {
            "label": "Date",
            "labelKey": "date"
          },
          {
            "label": "Pacientes",
            "labelKey": "patient"
          },
          {
            "label": "Médicos",
            "labelKey": "doctor"
          },
          {
            "label": "Options",
            "labelKey": "options"
          }
        ]
      }
    ],
    "phraseKeys": [
      "edit_prescription",
      "prescription_list",
      "doctor",
      "patient",
      "case_history",
      "medication",
      "medication_from_pharmacist",
      "add_description",
      "description",
      "date",
      "options",
      "edit",
      "delete"
    ],
    "icons": [
      "icon-wrench",
      "icon-align-justify",
      "icon-wrench",
      "icon-trash"
    ],
    "moduleLink": "/farmacia",
    "path": "/sghc/pharmacist/manage_prescription"
  },
  {
    "id": "pharmacist-manage_profile",
    "route": "pharmacist/manage_profile",
    "role": "pharmacist",
    "action": "manage_profile",
    "file": "pharmacist/manage_profile.php",
    "title": "Meu perfil",
    "kind": "operational",
    "hasBox": true,
    "tabs": [
      {
        "id": "list",
        "label": "Meu perfil",
        "labelKey": "manage_profile"
      }
    ],
    "tables": [],
    "phraseKeys": [
      "manage_profile",
      "name",
      "email",
      "address",
      "phone",
      "update_profile",
      "change_password",
      "password",
      "new_password",
      "confirm_new_password",
      "update_password"
    ],
    "icons": [
      "icon-align-justify",
      "icon-lock"
    ],
    "moduleLink": "/configuracoes/aparencia",
    "path": "/sghc/pharmacist/manage_profile"
  }
];

export const BAYANNO_SCREEN_BY_ROUTE: Record<string, BayannoScreen> = Object.fromEntries(
  BAYANNO_SCREENS.map((s) => [s.route, s]),
);

export const BAYANNO_SCREEN_BY_PATH: Record<string, BayannoScreen> = Object.fromEntries(
  BAYANNO_SCREENS.map((s) => [s.path, s]),
);
