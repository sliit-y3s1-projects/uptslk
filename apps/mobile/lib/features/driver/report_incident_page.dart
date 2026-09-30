import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../../models/driver_assignment.dart';
import '../../services/auth_api_service.dart';
import '../../services/driver_api_service.dart';
import '../../state/auth_store.dart';

class ReportIncidentPage extends StatefulWidget {
  const ReportIncidentPage({
    super.key,
    required this.duty,
    required this.authStore,
    required this.service,
  });

  final DriverAssignment duty;
  final AuthStore authStore;
  final DriverApiService service;

  @override
  State<ReportIncidentPage> createState() => _ReportIncidentPageState();
}

class _ReportIncidentPageState extends State<ReportIncidentPage> {
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  String _type = 'Breakdown';
  String _severity = 'Medium';
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final title = _titleController.text.trim();
    final description = _descriptionController.text.trim();
    if (title.isEmpty || description.isEmpty) {
      setState(() => _error = 'Enter a summary and describe what happened.');
      return;
    }
    final token = widget.authStore.token;
    if (token == null || token.isEmpty) {
      setState(() => _error = 'Your session has ended. Sign in again.');
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      await widget.service.reportIncident(
        token: token,
        tripId: widget.duty.id,
        type: _type,
        severity: _severity,
        title: title,
        description: description,
      );
      if (mounted) Navigator.of(context).pop(true);
    } on AuthApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'Could not send the report. Try again.');
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppTheme.surface,
    appBar: AppBar(title: const Text('Report incident')),
    body: ListView(
      padding: const EdgeInsets.fromLTRB(22, 18, 22, 28),
      children: [
        Text(
          '${widget.duty.routeNumber} · ${widget.duty.origin} to ${widget.duty.destination}',
          style: const TextStyle(
            color: AppTheme.brandPrimary,
            fontSize: 14,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          'Centre Ops will review this report. The trip status will not change unless you update it separately.',
          style: TextStyle(color: AppTheme.muted, height: 1.4),
        ),
        const SizedBox(height: 28),
        const _FieldLabel('What happened?'),
        const SizedBox(height: 10),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final type in const ['Breakdown', 'Delay', 'Safety', 'Other'])
              ChoiceChip(
                label: Text(type),
                selected: _type == type,
                onSelected: (_) => setState(() => _type = type),
                selectedColor: AppTheme.brandLight,
                side: BorderSide(
                  color: _type == type
                      ? AppTheme.brandPrimary
                      : AppTheme.borderStrong,
                ),
                labelStyle: TextStyle(
                  color: _type == type ? AppTheme.brandPrimary : AppTheme.ink,
                ),
              ),
          ],
        ),
        const SizedBox(height: 24),
        const _FieldLabel('Severity'),
        const SizedBox(height: 10),
        Wrap(
          spacing: 8,
          children: [
            for (final severity in const ['Low', 'Medium', 'High'])
              ChoiceChip(
                label: Text(severity),
                selected: _severity == severity,
                onSelected: (_) => setState(() => _severity = severity),
                selectedColor: AppTheme.brandLight,
                side: BorderSide(
                  color: _severity == severity
                      ? AppTheme.brandPrimary
                      : AppTheme.borderStrong,
                ),
                labelStyle: TextStyle(
                  color: _severity == severity
                      ? AppTheme.brandPrimary
                      : AppTheme.ink,
                ),
              ),
          ],
        ),
        const SizedBox(height: 24),
        const _FieldLabel('Summary'),
        const SizedBox(height: 8),
        TextField(
          controller: _titleController,
          maxLength: 200,
          textInputAction: TextInputAction.next,
          decoration: const InputDecoration(
            hintText: 'Briefly describe the issue',
          ),
        ),
        const SizedBox(height: 12),
        const _FieldLabel('Details'),
        const SizedBox(height: 8),
        TextField(
          controller: _descriptionController,
          maxLength: 2000,
          minLines: 4,
          maxLines: 6,
          textInputAction: TextInputAction.newline,
          decoration: const InputDecoration(
            hintText: 'What happened and where is the bus now?',
          ),
        ),
        if (_error != null) ...[
          const SizedBox(height: 12),
          Text(_error!, style: const TextStyle(color: AppTheme.danger)),
        ],
      ],
    ),
    bottomNavigationBar: SafeArea(
      top: false,
      child: Container(
        padding: const EdgeInsets.fromLTRB(22, 12, 22, 18),
        decoration: const BoxDecoration(
          color: AppTheme.surface,
          border: Border(top: BorderSide(color: AppTheme.border)),
        ),
        child: FilledButton(
          onPressed: _submitting ? null : _submit,
          child: _submitting
              ? const SizedBox.square(
                  dimension: 19,
                  child: CircularProgressIndicator(
                    color: Colors.white,
                    strokeWidth: 2,
                  ),
                )
              : const Text('Send report'),
        ),
      ),
    ),
  );
}

class _FieldLabel extends StatelessWidget {
  const _FieldLabel(this.text);
  final String text;

  @override
  Widget build(BuildContext context) => Text(
    text,
    style: const TextStyle(
      color: AppTheme.ink,
      fontSize: 15,
      fontWeight: FontWeight.w600,
    ),
  );
}
