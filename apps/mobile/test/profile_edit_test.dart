import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/theme/app_theme.dart';
import 'package:mobile/features/auth/auth_widgets.dart';
import 'package:mobile/features/profile/profile_page.dart';
import 'package:mobile/models/app_user.dart';
import 'package:mobile/services/auth_api_service.dart';
import 'package:mobile/state/auth_store.dart';

void main() {
  test('API gender labels are normalised without dropping unknown values', () {
    for (final entry in <String, String?>{
      ' male ': 'Male',
      'FEMALE': 'Female',
      'other': 'Other',
      'prefer not to say': 'Prefer not to say',
      'Custom value': 'Custom value',
      ' ': null,
    }.entries) {
      expect(AppUser.fromJson({'gender': entry.key}).gender, entry.value);
    }
    expect(AppUser.fromJson({}).gender, isNull);
  });

  testWidgets('Profile editing retains API gender and uses styled inputs', (
    tester,
  ) async {
    final auth = AuthStore(AuthApiService());
    addTearDown(auth.dispose);
    final user = AppUser.fromJson({
      'id': 'user',
      'name': 'Test Driver',
      'email': 'driver@example.test',
      'role': 'Driver',
      'gender': ' other ',
    });
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.lightTheme,
        home: ProfilePage(
          authStore: auth,
          user: user,
          showJourneyAction: false,
        ),
      ),
    );
    await tester.tap(find.text('Edit'));
    await tester.pumpAndSettle();
    expect(find.byType(AuthField), findsNWidgets(3));
    expect(find.byType(DropdownButtonFormField<String>), findsNothing);
    await tester.ensureVisible(find.text('Other'));
    await tester.tap(find.text('Other'));
    await tester.pumpAndSettle();
    expect(find.widgetWithText(ListTile, 'Other'), findsOneWidget);
    await tester.tap(find.widgetWithText(ListTile, 'Female'));
    await tester.pumpAndSettle();
    expect(find.text('Female'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
