
DELETE FROM [dbo].[Rooms];
DBCC CHECKIDENT ('[dbo].[Rooms]', RESEED, 0);


INSERT INTO [dbo].[Rooms] ([Gender], [RoomNumber], [Building], [Capacity], [Occupied], [Price], [Status])
VALUES

(N'Male', N'A0101', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0102', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0103', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0104', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0105', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0106', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0107', N'A', 8, 0, 800000.00, N'Available'),
(N'Male', N'A0108', N'A', 8, 0, 800000.00, N'Available'),

(N'Male', N'A0201', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0202', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0203', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0204', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0205', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0206', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0207', N'A', 6, 0, 1000000.00, N'Available'),
(N'Male', N'A0208', N'A', 6, 0, 1000000.00, N'Available'),

(N'Male', N'A0301', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0302', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0303', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0304', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0305', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0306', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0307', N'A', 4, 0, 1200000.00, N'Available'),
(N'Male', N'A0308', N'A', 4, 0, 1200000.00, N'Available'),


(N'Female', N'B0101', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0102', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0103', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0104', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0105', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0106', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0107', N'B', 8, 0, 800000.00, N'Available'),
(N'Female', N'B0108', N'B', 8, 0, 800000.00, N'Available'),

(N'Female', N'B0201', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0202', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0203', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0204', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0205', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0206', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0207', N'B', 6, 0, 1000000.00, N'Available'),
(N'Female', N'B0208', N'B', 6, 0, 1000000.00, N'Available'),

(N'Female', N'B0301', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0302', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0303', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0304', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0305', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0306', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0307', N'B', 4, 0, 1200000.00, N'Available'),
(N'Female', N'B0308', N'B', 4, 0, 1200000.00, N'Available');
