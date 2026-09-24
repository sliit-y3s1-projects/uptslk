import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/main.dart';

void main() {
  testWidgets('App loads Milestone 1 screen', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const UPTSLKApp());

    // Verify that the text exists
    expect(find.textContaining('Ready for the Login Page'), findsOneWidget);
  });
}
