import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:sistema_hospitalar_mobile/screens/appointments_screen.dart';
import 'package:sistema_hospitalar_mobile/screens/medical_record_screen.dart';
import 'package:sistema_hospitalar_mobile/services/api_service.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.api, required this.onLogout});

  final ApiService api;
  final VoidCallback onLogout;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Map<String, dynamic>? _dashboard;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await widget.api.getDashboard();
      setState(() => _dashboard = data);
    } catch (e) {
      setState(() => _error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    final dateFmt = DateFormat('dd/MM/yyyy HH:mm');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Meu Portal'),
        actions: [
          IconButton(icon: const Icon(Icons.logout), onPressed: widget.onLogout),
        ],
      ),
      body: _error != null
          ? Center(child: Text(_error!))
          : _dashboard == null
              ? const Center(child: CircularProgressIndicator())
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      Text(
                        'Olá, ${(_dashboard!['patientName'] as String).split(' ').first}',
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      Text('PEP: ${_dashboard!['recordNumber'] ?? '—'}'),
                      const SizedBox(height: 24),
                      _SectionTitle('Próximos agendamentos'),
                      ...(_dashboard!['upcomingAppointments'] as List<dynamic>).map((a) {
                        final dt = DateTime.parse(a['scheduledAt'] as String);
                        return Card(
                          child: ListTile(
                            leading: const Icon(Icons.event),
                            title: Text(a['professionalName'] as String),
                            subtitle: Text('${a['specialtyName']} · ${dateFmt.format(dt.toLocal())}'),
                          ),
                        );
                      }),
                      if ((_dashboard!['upcomingAppointments'] as List).isEmpty)
                        const Text('Nenhum agendamento futuro.'),
                      const SizedBox(height: 16),
                      _SectionTitle('Resultados recentes'),
                      ...(_dashboard!['recentLabResults'] as List<dynamic>).map((r) {
                        return Card(
                          color: (r['isAbnormal'] as bool?) == true ? Colors.red.shade50 : null,
                          child: ListTile(
                            title: Text(r['examName'] as String),
                            subtitle: Text('${r['value']} · Ref: ${r['referenceRange'] ?? '—'}'),
                          ),
                        );
                      }),
                      if ((_dashboard!['recentLabResults'] as List).isEmpty)
                        const Text('Nenhum resultado disponível.'),
                    ],
                  ),
                ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: 0,
        destinations: const [
          NavigationDestination(icon: Icon(Icons.home), label: 'Início'),
          NavigationDestination(icon: Icon(Icons.calendar_month), label: 'Agenda'),
          NavigationDestination(icon: Icon(Icons.folder_shared), label: 'Prontuário'),
        ],
        onDestinationSelected: (index) {
          if (index == 1) {
            Navigator.push(context, MaterialPageRoute(
              builder: (_) => AppointmentsScreen(api: widget.api),
            ));
          } else if (index == 2) {
            Navigator.push(context, MaterialPageRoute(
              builder: (_) => MedicalRecordScreen(api: widget.api),
            ));
          }
        },
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(text, style: Theme.of(context).textTheme.titleMedium),
    );
  }
}
