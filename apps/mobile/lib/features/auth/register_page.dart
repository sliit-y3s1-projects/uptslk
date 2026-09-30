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
      title: 'Create account',
      onBack: () {
        if (!_isSubmitting) Navigator.of(context).pop();
      },
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
              textCapitalization: TextCapitalization.words,
              autofillHints: const [AutofillHints.name],
            ),
            const SizedBox(height: 18),
            AuthField(
              controller: _emailController,
              label: 'Email address',
              hint: 'you@example.com',
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
              obscureText: _isConfirmPasswordHidden,
              textInputAction: TextInputAction.done,
              onSubmitted: (_) {
                if (!_isSubmitting) _submit();
              },
              onToggleVisibility: () {
                setState(
                  () => _isConfirmPasswordHidden = !_isConfirmPasswordHidden,
                );
              },
            ),
            const SizedBox(height: 30),
            PrimaryAuthButton(
              label: 'Create account',
              isLoading: _isSubmitting,
              onPressed: _isSubmitting ? null : _submit,
            ),
            const SizedBox(height: 24),
            const Divider(height: 1),
            const SizedBox(height: 18),
            Wrap(
              alignment: WrapAlignment.center,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                const Text(
                  'Already have an account?',
                  style: TextStyle(color: Color(0xFF6F7078)),
                ),
                TextButton(
                  onPressed: _isSubmitting
                      ? null
                      : () => Navigator.of(context).pop(),
                  child: const Text('Sign in'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
