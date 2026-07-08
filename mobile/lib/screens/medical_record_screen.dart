import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:sistema_hospitalar_mobile/services/api_service.dart';

class MedicalRecordScreen extends StatefulWidget {
  const MedicalRecordScreen({super.key, required this.api});

  final ApiService api;

  @override
  State<MedicalRecordScreen> createState() => _MedicalRecordScreenState();
}

class _MedicalRecordScreenState extends State<MedicalRecordScreen> {
  Map<String, dynamic>? _record;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final data = await widget.api.getMedicalRecord();
      setState(() => _record = data);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final dateFmt = DateFormat('dd/MM/yyyy HH:mm');

    return Scaffold(
      appBar: AppBar(title: const Text('Prontuário')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text(_error!))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    Text('PEP: ${_record!['recordNumber']}', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 16),
                    ...(_record!['entries'] as List<dynamic>).map((e) {
                      final entry = e as Map<String, dynamic>;
                      final dt = DateTime.parse(entry['createdAt'] as String);
                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                '${entry['entryType']} · ${dateFmt.format(dt.toLocal())}',
                                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
                              ),
                              if (entry['professionalName'] != null)
                                Text(entry['professionalName'] as String, style: TextStyle(color: Colors.grey[600], fontSize: 12)),
                              const SizedBox(height: 8),
                              Text(entry['content'] as String),
                            ],
                          ),
                        ),
                      );
                    }),
                    if ((_record!['entries'] as List).isEmpty)
                      const Text('Nenhuma entrada no prontuário.'),
                  ],
                ),
    );
  }
}
