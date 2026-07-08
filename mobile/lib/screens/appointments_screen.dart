import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:sistema_hospitalar_mobile/services/api_service.dart';

class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key, required this.api});

  final ApiService api;

  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  List<dynamic> _appointments = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final dashboard = await widget.api.getDashboard();
    setState(() {
      _appointments = dashboard['upcomingAppointments'] as List<dynamic>;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final dateFmt = DateFormat('dd/MM/yyyy HH:mm');

    return Scaffold(
      appBar: AppBar(title: const Text('Agendamentos')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView.builder(
              itemCount: _appointments.length,
              itemBuilder: (_, i) {
                final a = _appointments[i] as Map<String, dynamic>;
                final dt = DateTime.parse(a['scheduledAt'] as String);
                return ListTile(
                  leading: const Icon(Icons.medical_services),
                  title: Text(a['professionalName'] as String),
                  subtitle: Text('${a['specialtyName']}\n${dateFmt.format(dt.toLocal())}'),
                  isThreeLine: true,
                );
              },
            ),
    );
  }
}
