import 'package:flutter/material.dart';

import '../../state/auth_store.dart';
import 'auth_widgets.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key, required this.authStore});

  final AuthStore authStore;

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _isPasswordHidden = true;
  bool _isConfirmPasswordHidden = true;
  bool _isSubmitting = false;
  String? _error;

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final name = _nameController.text.trim();
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    if (name.isEmpty || email.isEmpty || password.isEmpty) {
      setState(
        () => _error = 'Complete your name, email address, and password.',
      );
      return;
    }
    if (!email.contains('@')) {
      setState(() => _error = 'Enter a valid email address.');
      return;
    }
    if (password.length < 8) {
      setState(() => _error = 'Use a password with at least 8 characters.');
      return;
    }
    if (password != _confirmPasswordController.text) {
      setState(() => _error = 'Your passwords do not match.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _error = null;
    });
    try {
      await widget.authStore.signUp(
        name: name,
        email: email,
        password: password,
      );
      if (mounted) Navigator.of(context).pop();
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthFrame(
      title: 'Create your account',
      child: AutofillGroup(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_error != null) ...[
              AuthErrorMessage(message: _error!),
              const SizedBox(height: 18),
            ],
            AuthField(
              controller: _nameController,
              label: 'Full name',
              hint: 'Your name',
              icon: Icons.person_outline_rounded,
              autofillHints: const [AutofillHints.name],
            ),
            const SizedBox(height: 18),
            AuthField(
              controller: _emailController,
              label: 'Email address',
              hint: 'you@example.com',
              icon: Icons.alternate_email_rounded,
              keyboardType: TextInputType.emailAddress,
              autofillHints: const [
                AutofillHints.username,
                AutofillHints.email,
              ],
            ),
            const SizedBox(height: 18),
            AuthField(
              controller: _passwordController,
              label: 'Password',
              hint: 'At least 8 characters',
              icon: Icons.lock_outline_rounded,
              obscureText: _isPasswordHidden,
              onToggleVisibility: () {
                setState(() => _isPasswordHidden = !_isPasswordHidden);
              },
              autofillHints: const [AutofillHints.newPassword],
            ),
            const SizedBox(height: 18),
            AuthField(
              controller: _confirmPasswordController,
              label: 'Confirm password',
              hint: 'Re-enter your password',
              icon: Icons.verified_user_outlined,
              obscureText: _isConfirmPasswordHidden,
              onToggleVisibility: () {
                setState(
                  () => _isConfirmPasswordHidden = !_isConfirmPasswordHidden,
                );
              },
            ),
            const SizedBox(height: 28),
            PrimaryAuthButton(
              label: 'Create commuter account',
              isLoading: _isSubmitting,
              onPressed: _isSubmitting ? null : _submit,
            ),
            const SizedBox(height: 22),
            TextButton(
              onPressed: _isSubmitting
                  ? null
                  : () => Navigator.of(context).pop(),
              child: const Text('I already have an account'),
            ),
          ],
        ),
      ),
    );
  }
}
