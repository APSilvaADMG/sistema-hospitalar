# Inventário Bayanno HMS (SGHC)

Gerado em: 2026-06-11T15:18:24.199Z
Fonte: `Diversos/Scripts/_extracted/sghc-php/hospitalar`

## Resumo

- Telas (views): **73**
- Perfis com menu: **7**
- Controllers: **10**
- Frases no SQL: **20**
- Classes CSS distintas nas views: **148**

## Menus por perfil

### accountant

- `accountant/dashboard` — dashboard (icon-desktop icon-2x)
- `accountant/manage_invoice` — invoice / take_payment (icon-list-alt icon-2x)
- `accountant/view_payment` — view_payment (icon-money icon-2x)
- `accountant/manage_profile` — profile (icon-lock icon-2x)

### admin

- `admin/dashboard` — dashboard (icon-desktop icon-2x)
- `admin/manage_department` — department (icon-sitemap icon-2x)
- `admin/manage_doctor` — doctor (icon-user-md icon-2x)
- `admin/manage_patient` — patient (icon-user icon-2x)
- `admin/manage_nurse` — nurse (icon-plus-sign-alt icon-2x)
- `admin/manage_pharmacist` — pharmacist (icon-medkit icon-2x)
- `admin/manage_laboratorist` — laboratorist (icon-beaker icon-2x)
- `admin/manage_accountant` — accountant (icon-money icon-2x)
- **monitor_hospital** (submenu)
- `admin/view_appointment` — view_appointment (icon-exchange)
- `admin/view_payment` — view_payment (icon-money)
- `admin/view_bed_status` — view_bed_status (icon-hdd)
- `admin/view_blood_bank` — view_blood_bank (icon-tint)
- `admin/view_medicine` — view_medicine (icon-medkit)
- `admin/view_report/operation` — view_operation (icon-reorder)
- `admin/view_report/birth` — view_birth_report (icon-github-alt)
- `admin/view_report/death` — view_death_report (icon-user)
- **settings** (submenu)
- `admin/manage_email_template` — manage_email_template (icon-envelope)
- `admin/manage_noticeboard` — manage_noticeboard (icon-columns)
- `admin/system_settings` — system_settings (icon-h-sign)
- `admin/manage_language` — manage_language (icon-globe)
- `admin/backup_restore` — backup_restore (icon-download-alt)
- `admin/manage_profile` — profile (icon-lock icon-2x)

### doctor

- `doctor/dashboard` — dashboard (icon-desktop icon-2x)
- `doctor/manage_patient` — patient (icon-user icon-2x)
- `doctor/manage_appointment` — manage_appointment (icon-edit icon-2x)
- `doctor/manage_prescription` — manage_prescription (icon-stethoscope icon-2x)
- `doctor/manage_bed_allotment` — bed_allotment (icon-hdd icon-2x)
- `doctor/view_blood_bank` — view_blood_bank (icon-tint icon-2x)
- `doctor/manage_report` — manage_report (icon-hospital icon-2x)
- `doctor/manage_profile` — profile (icon-lock icon-2x)

### laboratorist

- `laboratorist/dashboard` — dashboard (icon-desktop icon-2x)
- `laboratorist/manage_prescription` — add_diagnosis_report (icon-stethoscope icon-2x)
- `laboratorist/manage_blood_bank` — manage_blood_bank (icon-tint icon-2x)
- `laboratorist/manage_blood_donor` — manage_blood_donor (icon-user icon-2x)
- `laboratorist/manage_profile` — profile (icon-lock icon-2x)

### nurse

- `nurse/dashboard` — dashboard (icon-desktop icon-2x)
- `nurse/manage_patient` — patient (icon-user icon-2x)
- **bed_ward** (submenu)
- `nurse/manage_bed` — manage_bed (icon-hdd)
- `nurse/manage_bed_allotment` — manage_bed_allotment (icon-wrench)
- **blood_bank** (submenu)
- `nurse/manage_blood_bank` — manage_blood_bank (icon-tint)
- `nurse/manage_blood_donor` — manage_blood_donor (icon-user)
- `nurse/manage_report` — report (icon-hospital icon-2x)
- `nurse/manage_profile` — profile (icon-lock icon-2x)

### patient

- `patient/dashboard` — dashboard (icon-desktop icon-2x)
- `patient/view_appointment` — view_appointment (icon-edit icon-2x)
- `patient/view_prescription` — view_prescription (icon-stethoscope icon-2x)
- `patient/view_doctor` — view_doctor (icon-user-md icon-2x)
- `patient/view_blood_bank` — view_blood_bank (icon-tint icon-2x)
- `patient/view_admit_history` — admit_history (icon-hdd icon-2x)
- `patient/view_operation_history` — operation_history (icon-hospital icon-2x)
- `patient/view_invoice` — view_invoice (icon-credit-card icon-2x)
- `patient/payment_history` — payment_history (icon-money icon-2x)
- `patient/manage_profile` — profile (icon-lock icon-2x)

### pharmacist

- `pharmacist/dashboard` — dashboard (icon-desktop icon-2x)
- `pharmacist/manage_medicine_category` — medicine_category (icon-edit icon-2x)
- `pharmacist/manage_medicine` — manage_medicine (icon-medkit icon-2x)
- `pharmacist/manage_prescription` — provide_medication (icon-stethoscope icon-2x)
- `pharmacist/manage_profile` — profile (icon-lock icon-2x)

## Telas com abas

### accountant/manage_invoice.php

Abas: edit_invoice | invoice_list | add_invoice

### accountant/manage_profile.php

Abas: manage_profile

### accountant/payment_history.php

Abas: view_payment

### accountant/take_cash_payment.php

Abas: take_cash_payment

### accountant/view_invoice.php

Abas: invoice_list

### accountant/view_payment.php

Abas: view_payment

### admin/backup_restore.php

Abas: backup | restore

### admin/manage_accountant.php

Abas: edit_accountant | accountant_list | add_accountant

### admin/manage_department.php

Abas: edit_department | department_list | add_department

### admin/manage_doctor.php

Abas: edit_doctor | doctor_list | add_doctor

### admin/manage_laboratorist.php

Abas: edit_laboratorist | laboratorist_list | add_laboratorist

### admin/manage_language.php

Abas: phrase_list | add_phrase | add_language

### admin/manage_noticeboard.php

Abas: edit_noticeboard | noticeboard_list | add_noticeboard

### admin/manage_nurse.php

Abas: edit_nurse | nurse_list | add_nurse

### admin/manage_patient.php

Abas: edit_patient | patient_list | add_patient

### admin/manage_pharmacist.php

Abas: edit_pharmacist | pharmacist_list | add_pharmacist

### admin/manage_profile.php

Abas: manage_profile

### admin/system_settings.php

Abas: system_settings

### admin/view_appointment.php

Abas: view_appointment

### admin/view_bed_status.php

Abas: bed_allotment | bed_list

### admin/view_blood_bank.php

Abas: blood_donor_list | blood_bank

### admin/view_log.php

Abas: view_log

### admin/view_medicine.php

Abas: view_medicine

### admin/view_payment.php

Abas: view_payment

### admin/view_report.php

Abas: view_report

### doctor/manage_appointment.php

Abas: edit_appointment | appointment_list | add_appointment

### doctor/manage_bed_allotment.php

Abas: edit_bed_allotment | bed_allotment_list | add_bed_allotment

### doctor/manage_patient.php

Abas: edit_patient | patient_list | add_patient

### doctor/manage_prescription.php

Abas: edit_prescription | prescription_list | add_prescription

### doctor/manage_profile.php

Abas: manage_profile

### doctor/manage_report.php

Abas: operation | birth | death | other | add_report

### doctor/view_blood_bank.php

Abas: blood_donor_list | blood_bank

### laboratorist/manage_blood_bank.php

Abas: edit_blood_bank | blood_bank_list

### laboratorist/manage_blood_donor.php

Abas: edit_blood_donor | blood_donor_list | add_blood_donor

### laboratorist/manage_prescription.php

Abas: edit_prescription | prescription_list

### laboratorist/manage_profile.php

Abas: manage_profile

### laboratorist/view_blood_bank.php

Abas: blood_donor_list | blood_bank

### nurse/manage_bed_allotment.php

Abas: edit_bed_allotment | bed_allotment_list | add_bed_allotment

### nurse/manage_bed.php

Abas: edit_bed | bed_list | add_bed

### nurse/manage_blood_bank.php

Abas: edit_blood_bank | blood_bank_list

### nurse/manage_blood_donor.php

Abas: edit_blood_donor | blood_donor_list | add_blood_donor

### nurse/manage_patient.php

Abas: edit_patient | patient_list | add_patient

### nurse/manage_profile.php

Abas: manage_profile

### nurse/manage_report.php

Abas: operation | birth | death | other | add_report

### patient/manage_patient.php

Abas: edit_patient | patient_list | add_patient

### patient/manage_profile.php

Abas: manage_profile

### patient/payment_history.php

Abas: view_payment

### patient/view_admit_history.php

Abas: bed_allotment_list

### patient/view_appointment.php

Abas: appointment_list

### patient/view_blood_bank.php

Abas: blood_donor_list | blood_bank

### patient/view_doctor.php

Abas: doctor_list

### patient/view_invoice.php

Abas: invoice_list

### patient/view_operation_history.php

Abas: operation | birth | other

### patient/view_prescription.php

Abas: edit_prescription | prescription_list

### pharmacist/manage_medicine_category.php

Abas: edit_medicine_category | medicine_category_list | add_medicine_category

### pharmacist/manage_medicine.php

Abas: edit_medicine | medicine_list | add_medicine

### pharmacist/manage_prescription.php

Abas: edit_prescription | prescription_list

### pharmacist/manage_profile.php

Abas: manage_profile

## Telas com tabela dTable

### accountant/manage_invoice.php

- Colunas: #, invoice_id, title, amount, patient, date, status, option

### accountant/payment_history.php

- Colunas: #, time, amount, payment_type, transaction_id, invoice_id, accountant, method, description

### accountant/view_invoice.php

- Colunas: #, invoice_id, amount, accountant, title, description, creation_timestamp, status, option

### accountant/view_payment.php

- Colunas: #, time, amount, payment_type, transaction_id, invoice_id, patient, method, description

### admin/manage_accountant.php

- Colunas: #, accountant_name, email, address, phone, options

### admin/manage_department.php

- Colunas: #, department_name, description, options

### admin/manage_doctor.php

- Colunas: #, doctor_name, department, options

### admin/manage_laboratorist.php

- Colunas: #, laboratorist_name, email, address, phone, options

### admin/manage_noticeboard.php

- Colunas: #, title, notice, date, options

### admin/manage_nurse.php

- Colunas: #, nurse_name, email, address, phone, options

### admin/manage_patient.php

- Colunas: #, patient_name, age, sex, blood_group, birth_date, options

### admin/manage_pharmacist.php

- Colunas: #, pharmacist_name, email, address, phone, options

### admin/view_appointment.php

- Colunas: #, time, doctor, patient

### admin/view_bed_status.php

- Colunas: #, bed_id, bed_type, patient, allotment_time, discharge_time

- Colunas: #, bed_number, type

### admin/view_blood_bank.php

- Colunas: #, name, age, sex, blood_group, last_donation_date

- Colunas: #, blood_group, status

### admin/view_log.php

- Colunas: #, type, date, user, name, description, ip, location

### admin/view_medicine.php

- Colunas: #, name, category, description, price, manufacturing_company

### admin/view_payment.php

- Colunas: #, time, amount, payment_type, transaction_id, invoice_id, patient, method, description

### admin/view_report.php

- Colunas: #, description, date, patient, doctor

### doctor/manage_appointment.php

- Colunas: #, date, patient, doctor, options

### doctor/manage_bed_allotment.php

- Colunas: #, bed_number, bed_type, patient, allotment_date_time, discharge_date_time, options

### doctor/manage_patient.php

- Colunas: #, patient_name, age, sex, blood_group, birth_date, options

### doctor/manage_prescription.php

- Colunas: #, date, patient, doctor, options

### doctor/manage_report.php

- Colunas: #, description, date, patient, doctor, options

- Colunas: #, description, date, patient, doctor, options

- Colunas: #, description, date, patient, doctor, options

- Colunas: #, description, date, patient, doctor, options

### doctor/view_blood_bank.php

- Colunas: #, name, age, sex, blood_group, last_donation_date

- Colunas: #, blood_group, status

### laboratorist/manage_blood_bank.php

- Colunas: #, blood_group, status, options

### laboratorist/manage_blood_donor.php

- Colunas: #, name, age, sex, blood_group, last_donation_date, options

### laboratorist/manage_prescription.php

- Colunas: #, date, patient, doctor, report_status, options

### laboratorist/view_blood_bank.php

- Colunas: #, name, age, sex, blood_group, last_donation_date

- Colunas: #, blood_group, status

### nurse/manage_bed_allotment.php

- Colunas: #, bed_number, bed_type, patient, allotment_date_time, discharge_date_time, options

### nurse/manage_bed.php

- Colunas: #, bed_number, type, options

### nurse/manage_blood_bank.php

- Colunas: #, blood_group, status, options

### nurse/manage_blood_donor.php

- Colunas: #, name, age, sex, blood_group, last_donation_date, options

### nurse/manage_patient.php

- Colunas: #, patient_name, age, sex, blood_group, birth_date, options

### nurse/manage_report.php

- Colunas: #, description, date, patient, doctor, options

- Colunas: #, description, date, patient, doctor, options

- Colunas: #, description, date, patient, doctor, options

- Colunas: #, description, date, patient, doctor, options

### patient/manage_patient.php

- Colunas: #, patient_name, age, sex, blood_group, birth_date, options

### patient/payment_history.php

- Colunas: #, time, amount, payment_type, transaction_id, invoice_id, patient, method, description

### patient/view_admit_history.php

- Colunas: #, bed_number, bed_type, patient, allotment_date_time, discharge_date_time, options

### patient/view_appointment.php

- Colunas: #, date, doctor, department

### patient/view_blood_bank.php

- Colunas: #, name, age, sex, blood_group, last_donation_date

- Colunas: #, blood_group, status

### patient/view_doctor.php

- Colunas: #, doctor_name, department

### patient/view_invoice.php

- Colunas: #, invoice_id, amount, patient, title, description, creation_timestamp, status, option

### patient/view_operation_history.php

- Colunas: #, description, date, patient, patient, options

- Colunas: #, description, date, patient, patient, options

- Colunas: #, description, date, patient, patient, options

### patient/view_prescription.php

- Colunas: #, date, patient, patient, options

### pharmacist/manage_medicine_category.php

- Colunas: #, medicine_category_name, description, options

### pharmacist/manage_medicine.php

- Colunas: #, medicine_name, medicine_catogory, description, price, manufacturing_company, status, options

### pharmacist/manage_prescription.php

- Colunas: #, date, patient, doctor, options

## Arquivos gerados

- `inventory.json` — inventário completo
- `views.json` — estrutura de cada tela
- `navigation.json` — menus laterais
- `routes.json` — rotas dos controllers
- `phrases.json` — traduções (SQL + uso nas views)
- `css-classes.json` — classes CSS usadas nas views
