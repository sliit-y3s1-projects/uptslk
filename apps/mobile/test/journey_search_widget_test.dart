import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/theme/app_theme.dart';
import 'package:mobile/features/commuter/commuter_home_page.dart';
import 'package:mobile/models/journey_option.dart';
import 'package:mobile/models/transit_centre.dart';
import 'package:mobile/services/auth_api_service.dart';
import 'package:mobile/services/trip_search_api_service.dart';
import 'package:mobile/state/auth_store.dart';

class _JourneyService extends TripSearchApiService {
  @override
  Future<List<TransitCentre>> getOperatingCentres() async => const [
    TransitCentre(
      id: 'a',
      name: 'Kadawatha Centre',
      city: 'Kadawatha',
      status: 'Operating',
    ),
    TransitCentre(
      id: 'b',
      name: 'Makumbura Centre',
      city: 'Makumbura',
      status: 'Operating',
    ),
  ];

  @override
  Future<List<JourneyOption>> getJourneyOptions() async => const [
    JourneyOption(
      id: 'direction',
      routeId: 'route',
      routeNumber: 'EX-KM-01',
      name: 'Kadawatha - Makumbura Express',
      origin: 'Kadawatha Centre',
      destination: 'Makumbura Centre',
    ),
  ];
}

void main() {
  for (final width in [320.0, 390.0]) {
    for (final scale in [1.0, 1.8]) {
      testWidgets(
        'Journey controls and routes fit at width $width, scale $scale',
        (tester) async {
          tester.view.physicalSize = Size(width, 844);
          tester.view.devicePixelRatio = 1;
          addTearDown(tester.view.resetPhysicalSize);
          addTearDown(tester.view.resetDevicePixelRatio);
          final auth = AuthStore(AuthApiService());
          addTearDown(auth.dispose);
          await tester.pumpWidget(
            MaterialApp(
              theme: AppTheme.lightTheme,
              builder: (context, child) => MediaQuery(
                data: MediaQuery.of(context)
                    .copyWith(textScaler: TextScaler.linear(scale)),
                child: child!,
              ),
              home: CommuterHomePage(
                firstName: 'Test',
                authStore: auth,
                service: _JourneyService(),
              ),
            ),
          );
          await tester.pumpAndSettle();
          expect(find.text('Journey details'), findsOneWidget);
          expect(tester.takeException(), isNull);
          await tester.ensureVisible(find.byTooltip('Add seat'));
          await tester.tap(find.byTooltip('Add seat'));
          await tester.pumpAndSettle();
          expect(find.text('2'), findsOneWidget);
          await tester.ensureVisible(find.text('View departures'));
          await tester.pumpAndSettle();
          expect(find.text('Kadawatha Centre'), findsOneWidget);
          expect(tester.takeException(), isNull);
        },
      );
    }
  }
}
