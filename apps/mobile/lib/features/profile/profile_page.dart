import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../core/theme/app_theme.dart';
import '../../models/app_user.dart';
import '../../services/auth_api_service.dart';
import '../../state/auth_store.dart';
import '../commuter/commuter_home_page.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({
    super.key,
    required this.authStore,
    required this.user,
    this.showJourneyAction = true,
  });

  final AuthStore authStore;
  final AppUser user;
  final bool showJourneyAction;

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  final _nameController = TextEditingController();
  final _locationController = TextEditingController();
  final _nicController = TextEditingController();
  final _picker = ImagePicker();
  String? _gender;
  bool _editing = false;
  bool _saving = false;
  bool _uploadingPhoto = false;

  @override
  void initState() {
    super.initState();
    _syncFields(widget.user);
  }

  @override
  void didUpdateWidget(covariant ProfilePage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!_editing && oldWidget.user != widget.user) _syncFields(widget.user);
  }

  @override
  void dispose() {
    _nameController.dispose();
    _locationController.dispose();
    _nicController.dispose();
    super.dispose();
  }

  void _syncFields(AppUser user) {
    _nameController.text = user.name;
    _locationController.text = user.homeLocation ?? '';
    _nicController.text = user.nicNumber ?? '';
    _gender = _dropdownGender(user.gender);
  }

  Future<void> _pickPhoto() async {
    final photo = await _picker.pickImage(
      source: ImageSource.gallery,
      maxWidth: 1200,
      imageQuality: 84,
    );
    if (photo == null || !mounted) return;

    setState(() => _uploadingPhoto = true);
    try {
      final bytes = await photo.readAsBytes();
      final contentType = _contentTypeFor(photo);
      if (!const {
        'image/jpeg',
        'image/png',
        'image/webp',
      }.contains(contentType)) {
        throw const AuthApiException('Choose a JPG, PNG, or WebP image.');
      }
      if (bytes.length > 5 * 1024 * 1024) {
        throw const AuthApiException('Profile images cannot exceed 5 MB.');
      }
      await widget.authStore.uploadProfilePhoto(
        bytes: bytes,
        fileName: photo.name,
        contentType: contentType,
      );
      if (mounted) _showMessage('Profile photo updated.');
    } on AuthApiException catch (error) {
      if (mounted) _showMessage(error.message, isError: true);
    } catch (_) {
      if (mounted) {
        _showMessage('Could not update your profile photo.', isError: true);
      }
    } finally {
      if (mounted) setState(() => _uploadingPhoto = false);
    }
  }

  Future<void> _saveProfile() async {
    final name = _nameController.text.trim();
    if (name.isEmpty) {
      _showMessage('Your full name is required.', isError: true);
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.authStore.updateProfile(
        name: name,
        homeLocation: _emptyToNull(_locationController.text),
        nicNumber: _emptyToNull(_nicController.text),
        gender: _gender,
      );
      if (!mounted) return;
      setState(() => _editing = false);
      _showMessage('Profile updated.');
    } on AuthApiException catch (error) {
      if (mounted) _showMessage(error.message, isError: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _cancelEditing() {
    _syncFields(widget.user);
    setState(() => _editing = false);
  }

  String? _emptyToNull(String value) {
    final trimmed = value.trim();
    return trimmed.isEmpty ? null : trimmed;
  }

  String _contentTypeFor(XFile file) {
    if (file.mimeType != null && file.mimeType!.isNotEmpty) {
      return file.mimeType!.toLowerCase();
    }
    final extension = file.name.split('.').last.toLowerCase();
    return switch (extension) {
      'jpg' || 'jpeg' => 'image/jpeg',
      'png' => 'image/png',
      'webp' => 'image/webp',
      _ => '',
    };
  }

  void _showMessage(String message, {bool isError = false}) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: isError ? AppTheme.danger : const Color(0xFF047857),
        ),
      );
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.user;
    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        title: const Text('Profile'),
        actions: [
          if (_editing)
            TextButton(
              onPressed: _saving ? null : _cancelEditing,
              child: const Text('Cancel'),
            )
          else
            TextButton.icon(
              onPressed: () => setState(() => _editing = true),
              icon: const Icon(Icons.edit_outlined, size: 17),
              label: const Text('Edit'),
            ),
          const SizedBox(width: 10),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 22, 20, 32),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _ProfileIdentity(
                    user: user,
                    uploadingPhoto: _uploadingPhoto,
                    onPhotoTap: _uploadingPhoto ? null : _pickPhoto,
                  ),
                  const SizedBox(height: 16),
                  if (_editing)
                    _EditProfileForm(
                      nameController: _nameController,
                      locationController: _locationController,
                      nicController: _nicController,
                      gender: _gender,
                      saving: _saving,
                      onGenderChanged: (value) =>
                          setState(() => _gender = value),
                      onSave: _saveProfile,
                    )
                  else
                    _ProfileDetails(user: user),
                  const SizedBox(height: 24),
                  if (widget.showJourneyAction) ...[
                    FilledButton.icon(
                      onPressed: () => Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => CommuterHomePage(
                            firstName: user.name.split(' ').first,
                            authStore: widget.authStore,
                          ),
                        ),
                      ),
                      style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(54),
                        backgroundColor: AppTheme.brandPrimary,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                      icon: const Icon(Icons.search_rounded),
                      label: const Text('Find departures'),
                    ),
                    const SizedBox(height: 12),
                  ],
                  OutlinedButton.icon(
                    onPressed: () => widget.authStore.signOut(),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppTheme.danger,
                      side: const BorderSide(color: Color(0xFFE9B9BD)),
                    ),
                    icon: const Icon(Icons.logout_rounded, size: 19),
                    label: const Text('Sign out'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _ProfileIdentity extends StatelessWidget {
  const _ProfileIdentity({
    required this.user,
    required this.uploadingPhoto,
    required this.onPhotoTap,
  });

  final AppUser user;
  final bool uploadingPhoto;
  final VoidCallback? onPhotoTap;

  @override
  Widget build(BuildContext context) {
    final hasPhoto = user.profilePhotoUrl?.isNotEmpty ?? false;
    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.border),
        borderRadius: BorderRadius.circular(22),
      ),
      child: Row(
        children: [
          Stack(
            clipBehavior: Clip.none,
            children: [
              CircleAvatar(
                radius: 42,
                backgroundColor: AppTheme.brandLight,
                backgroundImage: hasPhoto
                    ? NetworkImage(user.profilePhotoUrl!)
                    : null,
                child: hasPhoto
                    ? null
                    : Text(
                        _initials(user.name),
                        style: const TextStyle(
                          color: AppTheme.brandPrimary,
                          fontSize: 24,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
              ),
              Positioned(
                right: -4,
                bottom: -4,
                child: Material(
                  color: AppTheme.brandPrimary,
                  shape: const CircleBorder(),
                  child: InkWell(
                    onTap: onPhotoTap,
                    customBorder: const CircleBorder(),
                    child: SizedBox(
                      width: 32,
                      height: 32,
                      child: uploadingPhoto
                          ? const Padding(
                              padding: EdgeInsets.all(8),
                              child: CircularProgressIndicator(
                                color: Colors.white,
                                strokeWidth: 2,
                              ),
                            )
                          : const Icon(
                              Icons.camera_alt_outlined,
                              color: Colors.white,
                              size: 17,
                            ),
                    ),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(width: 18),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  user.name,
                  style: const TextStyle(
                    color: AppTheme.ink,
                    fontSize: 21,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  user.email,
                  style: const TextStyle(color: AppTheme.muted, fontSize: 14),
                ),
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: AppTheme.brandLight,
                    borderRadius: BorderRadius.circular(99),
                  ),
                  child: Text(
                    user.roleLabel,
                    style: const TextStyle(
                      color: AppTheme.brandPrimary,
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ProfileDetails extends StatelessWidget {
  const _ProfileDetails({required this.user});
  final AppUser user;

  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: AppTheme.surface,
      border: Border.all(color: AppTheme.border),
      borderRadius: BorderRadius.circular(22),
    ),
    child: Column(
      children: [
        _DetailRow(
          icon: Icons.alternate_email_rounded,
          label: 'Email address',
          value: user.email,
        ),
        const Divider(height: 1, color: AppTheme.border),
        _DetailRow(
          icon: Icons.location_on_outlined,
          label: 'Home location',
          value: _display(user.homeLocation),
        ),
        const Divider(height: 1, color: AppTheme.border),
        _DetailRow(
          icon: Icons.badge_outlined,
          label: 'NIC number',
          value: _display(user.nicNumber),
        ),
        const Divider(height: 1, color: AppTheme.border),
        _DetailRow(
          icon: Icons.person_outline,
          label: 'Gender',
          value: _display(user.gender),
        ),
        const Divider(height: 1, color: AppTheme.border),
        _DetailRow(
          icon: Icons.verified_user_outlined,
          label: 'Identity verification',
          value: _verificationLabel(user.nicVerificationStatus),
        ),
      ],
    ),
  );
}

class _EditProfileForm extends StatelessWidget {
  const _EditProfileForm({
    required this.nameController,
    required this.locationController,
    required this.nicController,
    required this.gender,
    required this.saving,
    required this.onGenderChanged,
    required this.onSave,
  });

  final TextEditingController nameController;
  final TextEditingController locationController;
  final TextEditingController nicController;
  final String? gender;
  final bool saving;
  final ValueChanged<String?> onGenderChanged;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(20),
    decoration: BoxDecoration(
      color: AppTheme.surface,
      border: Border.all(color: AppTheme.border),
      borderRadius: BorderRadius.circular(22),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Text(
          'Personal information',
          style: TextStyle(
            color: AppTheme.ink,
            fontSize: 18,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 18),
        _ProfileField(
          controller: nameController,
          label: 'Full name',
          textCapitalization: TextCapitalization.words,
        ),
        const SizedBox(height: 14),
        _ProfileField(
          controller: locationController,
          label: 'Home location',
          textCapitalization: TextCapitalization.words,
        ),
        const SizedBox(height: 14),
        _ProfileField(
          controller: nicController,
          label: 'NIC number',
          textCapitalization: TextCapitalization.characters,
        ),
        const SizedBox(height: 14),
        DropdownButtonFormField<String>(
          initialValue: _dropdownGender(gender),
          decoration: _inputDecoration('Gender'),
          items: const [
            DropdownMenuItem(value: 'Female', child: Text('Female')),
            DropdownMenuItem(value: 'Male', child: Text('Male')),
            DropdownMenuItem(
              value: 'Prefer not to say',
              child: Text('Prefer not to say'),
            ),
          ],
          onChanged: saving ? null : onGenderChanged,
        ),
        const SizedBox(height: 20),
        FilledButton(
          onPressed: saving ? null : onSave,
          style: FilledButton.styleFrom(
            minimumSize: const Size.fromHeight(52),
            backgroundColor: AppTheme.brandPrimary,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
            ),
          ),
          child: saving
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(
                    color: Colors.white,
                    strokeWidth: 2,
                  ),
                )
              : const Text('Save changes'),
        ),
      ],
    ),
  );
}

class _ProfileField extends StatelessWidget {
  const _ProfileField({
    required this.controller,
    required this.label,
    required this.textCapitalization,
  });
  final TextEditingController controller;
  final String label;
  final TextCapitalization textCapitalization;

  @override
  Widget build(BuildContext context) => TextField(
    controller: controller,
    textCapitalization: textCapitalization,
    decoration: _inputDecoration(label),
  );
}

InputDecoration _inputDecoration(String label) =>
    InputDecoration(labelText: label);

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(17),
    child: Row(
      children: [
        Container(
          height: 38,
          width: 38,
          decoration: BoxDecoration(
            color: AppTheme.brandLight,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icon, size: 19, color: AppTheme.brandPrimary),
        ),
        const SizedBox(width: 13),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(color: AppTheme.muted, fontSize: 12),
              ),
              const SizedBox(height: 3),
              Text(
                value,
                style: const TextStyle(
                  color: AppTheme.ink,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

String _initials(String name) => name
    .trim()
    .split(RegExp(r'\s+'))
    .where((part) => part.isNotEmpty)
    .take(2)
    .map((part) => part[0])
    .join()
    .toUpperCase();
String _display(String? value) =>
    value == null || value.isEmpty ? 'Not set' : value;
String _verificationLabel(String? value) =>
    value == null || value.isEmpty || value == 'NotStarted'
    ? 'Not completed'
    : value;

String? _dropdownGender(String? value) {
  final normalized = value?.trim();
  return const {'Female', 'Male', 'Prefer not to say'}.contains(normalized)
      ? normalized
      : null;
}
