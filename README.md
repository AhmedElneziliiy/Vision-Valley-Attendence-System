# Vision Valley Attendance System

An enterprise-grade attendance management platform with facial recognition, real-time access control, and multi-tenant organizational support.

## Overview

Vision Valley Attendance System is a comprehensive solution for tracking employee/student attendance using biometric facial recognition technology. The system supports multiple organizations, branches, and departments with real-time notifications and secure device management.

## Features

### 🔐 Biometric Authentication
- **Facial Recognition**: Secure check-in/check-out using face verification
- **Multi-Image Support**: Store multiple face images per user for improved accuracy
- **Device Management**: UDID-based device tracking and authorization

### 📊 Attendance Management
- **Real-Time Tracking**: Live attendance monitoring with SignalR
- **Multiple Status Types**: Present, Absent, Late, Excused
- **Action Logging**: Check-in, Check-out, Break tracking
- **Timetable Integration**: Schedule-based attendance validation

### 🏢 Multi-Tenant Architecture
- **Organizations**: Support for multiple companies or institutions
- **Branches**: Physical location management
- **Departments**: Team/unit organization
- **Role-Based Access**: Privilege and permission management

### 🚪 Access Control
- **LAMP System**: Physical access point management
- **Real-Time Requests**: SignalR-powered access approval workflow
- **Audit Trail**: Complete access request history

### 📱 Dashboard Features
- **Admin Panel**: MVC-based management dashboard
- **PDF Export**: Generate attendance reports
- **Face Verification**: Manual verification interface
- **UDID Reset**: Device management tools

## Technology Stack

### Backend
- **ASP.NET Core**: Web API and MVC framework
- **Entity Framework Core**: ORM and database management
- **ASP.NET Identity**: Authentication and authorization
- **SignalR**: Real-time communication

### Biometrics
- **FaceRecognition.DotNet**: Face detection and recognition
- Custom face encoding storage and matching

### Database
- **SQL Server**: Primary data store
- **Entity Framework Migrations**: Schema versioning

## Project Structure

```
Vision-Valley-Attendence-System/
├── CoreProject/                  # Backend API and Business Logic
│   ├── Context/                  # EF Core DbContext
│   ├── Models/                   # Domain entities
│   │   ├── ApplicationUser.cs    # User with face data
│   │   ├── Attendance.cs         # Attendance records
│   │   ├── Lamp.cs               # Access control points
│   │   └── LampAccessRequest.cs  # Real-time access requests
│   ├── Repositories/             # Data access layer
│   ├── Services/                 # Business logic
│   ├── Hubs/                     # SignalR hubs
│   ├── ViewModels/               # API DTOs
│   ├── Helpers/                  # Utility classes
│   └── Migrations/               # EF Core migrations
├── MvcCoreProject/               # Admin Dashboard (MVC)
├── SQL_Scripts/                  # Database scripts
└── TestFaceVerification.ps1      # Testing utilities
```

## Domain Models

### User Management
- **ApplicationUser**: Extended identity user with biometric data, UDID, branch/department
- **UserImage**: Face images for recognition
- **Device**: Registered mobile/tablet devices
- **Privilege**: Role-based permissions

### Organization Structure
- **Organization**: Top-level entity (Company/Educational modes)
- **Branch**: Physical locations
- **Department**: Teams within branches
- **Timetable**: Work/class schedules

### Attendance System
- **Attendance**: Primary check-in/check-out records
- **AttendanceRecord**: Detailed event logs
- **AttendanceStatus**: Present/Absent/Late/Excused
- **AttendanceActionType**: CheckIn/CheckOut/Break

### Access Control
- **Lamp**: Physical access points (doors, gates)
- **LampAccessRequest**: Real-time access approval workflow

## Getting Started

### Prerequisites
- .NET 6.0 or higher
- SQL Server 2019 or higher
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/AhmedElneziliiy/Vision-Valley-Attendence-System.git
   cd Vision-Valley-Attendence-System
   ```

2. **Configure Database**
   - Update connection strings in `appsettings.json`
   - Run migrations:
     ```bash
     dotnet ef database update --project CoreProject
     ```

3. **Install Dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the Application**
   ```bash
   dotnet run --project MvcCoreProject
   ```

### Configuration

Update `appsettings.json` with your settings:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VisionValleyDB;Trusted_Connection=True;"
  },
  "FaceRecognition": {
    "Tolerance": 0.6,
    "Model": "large"
  }
}
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/verify-face` - Face verification

### Attendance
- `POST /api/attendance/checkin` - Check-in with face
- `POST /api/attendance/checkout` - Check-out
- `GET /api/attendance/records` - Get attendance history
- `GET /api/attendance/export-pdf` - Export to PDF

### Access Control
- `POST /api/lamp/request-access` - Request access to LAMP
- `POST /api/lamp/approve/{requestId}` - Approve access
- `POST /api/lamp/deny/{requestId}` - Deny access

### Device Management
- `POST /api/device/register` - Register device UDID
- `POST /api/device/reset-udid/{userId}` - Reset user's device

## SignalR Hubs

### LampAccessHub
Real-time access request notifications:
```javascript
connection.on("ReceiveAccessRequest", (request) => {
  // Handle incoming access request
});

connection.invoke("ApproveAccess", requestId);
```

## Development Roadmap

### Recent Updates (November 2025)
- ✅ SignalR LAMP access request system
- ✅ Face verification dashboard integration
- ✅ Database layer for face recognition
- ✅ UDID reset functionality
- ✅ PDF export feature
- ✅ FaceRecognition.DotNet integration

### Planned Features
- [ ] Mobile app (iOS/Android)
- [ ] QR code fallback authentication
- [ ] Advanced reporting and analytics
- [ ] Geofencing for location-based attendance
- [ ] Integration with HR systems
- [ ] Multi-language support

## Testing

Run face verification tests:
```powershell
.\TestFaceVerification.ps1
```

## Security Considerations

- Face encodings are stored securely in the database
- UDID-based device authorization prevents unauthorized access
- Role-based access control (RBAC) for all operations
- All API endpoints require authentication
- SignalR connections are authenticated

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is private and proprietary.

## Author

**Ahmed Elneziliiy**
- GitHub: [@AhmedElneziliiy](https://github.com/AhmedElneziliiy)
- Email: elneziliiyahmed@gmail.com
- Location: Alexandria, Egypt

## Acknowledgments

- FaceRecognition.DotNet for biometric capabilities
- ASP.NET Core team for the excellent framework
- SignalR for real-time communication

## Support

For issues and questions:
- Open an issue in the [GitHub repository](https://github.com/AhmedElneziliiy/Vision-Valley-Attendence-System/issues)
- Contact: elneziliiyahmed@gmail.com

---

**Built with ❤️ for Vision Valley**