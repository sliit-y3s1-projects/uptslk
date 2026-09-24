import 'package:flutter/material.dart';
import '../models/app_user.dart';
import '../models/booking.dart';
import '../models/driver_duty.dart';
import '../data/mock_data.dart';

class DemoStore extends ChangeNotifier {
  AppUser? currentUser;
  List<Booking> myBookings = [];
  List<DriverDuty> myDuties = [];

  void login(AppUser user) {
    currentUser = user;
    if (user.role == 'driver') {
      // Load mock duties for the driver
      myDuties = MockData.driverDuties.map((d) => 
        DriverDuty(
          id: d.id, 
          departure: d.departure, 
          vehiclePlate: d.vehiclePlate, 
          passengerCount: d.passengerCount, 
          status: d.status, 
          stops: d.stops
        )
      ).toList();
    }
    notifyListeners();
  }

  void logout() {
    currentUser = null;
    notifyListeners();
  }

  void updateDutyStatus(String dutyId, String newStatus) {
    final index = myDuties.indexWhere((d) => d.id == dutyId);
    if (index != -1) {
      final d = myDuties[index];
      // Create a new duty object with the updated status
      myDuties[index] = DriverDuty(
        id: d.id, 
        departure: d.departure, 
        vehiclePlate: d.vehiclePlate, 
        passengerCount: d.passengerCount, 
        status: newStatus, 
        stops: d.stops
      );
      notifyListeners();
    }
  }
}
