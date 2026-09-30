import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/main.dart';

void main() {
  testWidgets('App starts on the sign-in screen', (WidgetTester tester) async {
    await tester.pumpWidget(const UPTSLKApp());

    expect(find.text('Email address'), findsOneWidget);
    expect(find.text('Create account'), findsOneWidget);
  });
}
