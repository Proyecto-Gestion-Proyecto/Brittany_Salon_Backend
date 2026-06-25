CREATE DATABASE BRITTANYSALON

USE BRITTANYSALON


--Tabla Empleado
CREATE TABLE Employee (
    employeeId INT IDENTITY PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    email NVARCHAR(150) NOT NULL UNIQUE,
    phone NVARCHAR(20),
    passwordHash NVARCHAR(255) NOT NULL,
    specialty NVARCHAR(100),
    imageUrl NVARCHAR(255),
    isActive BIT DEFAULT 1,
    createdAt DATETIME DEFAULT GETDATE()
);


--Tabla Cliente
CREATE TABLE Client (
    clientId INT IDENTITY PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    email NVARCHAR(150) NOT NULL UNIQUE,
    phone NVARCHAR(20),
    passwordHash NVARCHAR(255) NOT NULL,
    pendingBalance DECIMAL(10,2) DEFAULT 0,
    imageUrl NVARCHAR(255),
    isActive BIT DEFAULT 1,
    createdAt DATETIME DEFAULT GETDATE()
);


--Tabla Reseña
CREATE TABLE Review (
    reviewId INT IDENTITY PRIMARY KEY,
    comment NVARCHAR(255),
    rating INT CHECK (Rating BETWEEN 1 AND 5),
    imageUrl NVARCHAR(255),
    response NVARCHAR(255),
    reviewDate DATE DEFAULT GETDATE(),
    clientId INT NOT NULL,
    employeeId INT NOT NULL,
    CONSTRAINT FK_Review_Client
        FOREIGN KEY (clientId) REFERENCES Client(clientId),
    CONSTRAINT FK_Review_Employee
        FOREIGN KEY (employeeId) REFERENCES Employee(employeeId)
)


--Tabla Servicio
CREATE TABLE Service (
    serviceId INT IDENTITY PRIMARY KEY,
    serviceName NVARCHAR(100) NOT NULL,
    serviceDescription NVARCHAR(255),
    price DECIMAL(10,2) NOT NULL,
    durationMinutes INT NOT NULL,
    imageUrl NVARCHAR(255),
    serviceType NVARCHAR(50),
    isActive BIT DEFAULT 1
);

--Tabla Empleado-Servicio
CREATE TABLE EmployeeService (
    employeeServiceId INT IDENTITY PRIMARY KEY,
    employeeId INT NOT NULL,
    serviceId INT NOT NULL,
    CONSTRAINT FK_EmployeeService_Employee
        FOREIGN KEY (employeeId) REFERENCES Employee(employeeId),
    CONSTRAINT FK_EmployeeService_Service
        FOREIGN KEY (serviceId) REFERENCES Service(serviceId)
);


--Tabla Cita
CREATE TABLE Appointment (
    appointmentId INT IDENTITY PRIMARY KEY,
    appointmentDate DATE NOT NULL,
    startTime DATETIME NOT NULL,
    endTime DATETIME NOT NULL,
    appointmentStatus NVARCHAR(50),
    totalCost DECIMAL(10,2),
    isActive BIT DEFAULT 1,
    clientId INT NOT NULL,
    CONSTRAINT FK_Appointment_Client
        FOREIGN KEY (clientId) REFERENCES Client(clientId)
);



--Tabla Cita-Servicio
CREATE TABLE AppointmentService (
    appointmentServiceId INT IDENTITY PRIMARY KEY,
    appointmentId INT NOT NULL,
    serviceId INT NOT NULL,
    servicePrice DECIMAL(10,2),
    CONSTRAINT FK_AppointmentService_Appointment
        FOREIGN KEY (appointmentId) REFERENCES Appointment(appointmentId),
    CONSTRAINT FK_AppointmentService_Service
        FOREIGN KEY (serviceId) REFERENCES Service(serviceId)
);


--Tabla Pago
CREATE TABLE Payment (
    paymentId INT IDENTITY PRIMARY KEY,
    appointmentId INT NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    paymentStatus NVARCHAR(50),
    paymentDate DATETIME DEFAULT GETDATE(),
    paymentMethod NVARCHAR(50),
    isActive BIT DEFAULT 1,
    CONSTRAINT FK_Payment_Appointment
        FOREIGN KEY (appointmentId) REFERENCES Appointment(appointmentId)
);



--Tabla Producto
CREATE TABLE Product (
    productId INT IDENTITY PRIMARY KEY,
    productName NVARCHAR(100) NOT NULL,
    productDescription NVARCHAR(255),
    price DECIMAL(10,2) NOT NULL,
    imageUrl NVARCHAR(255),
    expirationDate DATE,
    isActive BIT DEFAULT 1,
);


--Tabla Cita-Producto
CREATE TABLE AppointmentProduct (
    appointmentProductId INT IDENTITY PRIMARY KEY,
    appointmentId INT NOT NULL,
    productId INT NOT NULL,
    CONSTRAINT FK_AppointmentProduct_Appointment
        FOREIGN KEY (appointmentId) REFERENCES Appointment(appointmentId),
    CONSTRAINT FK_AppointmentProduct_Product
        FOREIGN KEY (productId) REFERENCES Product(productId)
);




--Tabla Inventario
CREATE TABLE Inventory (
    inventoryId INT IDENTITY PRIMARY KEY,
    productId INT NOT NULL UNIQUE,
    quantity INT NOT NULL,
    minimumStock INT NOT NULL,
    category NVARCHAR(255),
    isActive BIT DEFAULT 1,
    CONSTRAINT FK_Inventory_Product
        FOREIGN KEY (productId) REFERENCES Product(productId)
);




------------------
select * from Serviceselect * from Service


ALTER TABLE AppointmentProduct
ADD quantity INT NOT NULL DEFAULT 1;

ALTER TABLE Appointment
ADD hairLengthOption INT NULL;



ALTER TABLE Review
ALTER COLUMN employeeId INT NULL;


-- Paso 1 
-- Tabla categoria
CREATE TABLE Category (
    categoryId INT IDENTITY PRIMARY KEY,
    categoryName NVARCHAR(50) NOT NULL UNIQUE,
    categoryDescription NVARCHAR(255),
    isActive BIT DEFAULT 1
);

-- Paso 2 
ALTER TABLE Product
ADD categoryId INT NULL;

-- Paso 3
INSERT INTO Category (categoryName, categoryDescription)
VALUES ('General', 'Categoría por defecto para productos existentes');

SELECT categoryId FROM Category WHERE categoryName = 'General';

DECLARE @generalId INT;

SELECT @generalId = categoryId 
FROM Category 
WHERE categoryName = 'General';

UPDATE Product
SET categoryId = @generalId
WHERE categoryId IS NULL;


-- Paso 4 
ALTER TABLE Product
ALTER COLUMN categoryId INT NOT NULL;

-- Paso 5 
ALTER TABLE Product
ADD CONSTRAINT FK_Product_Category
FOREIGN KEY (categoryId) REFERENCES Category(categoryId);


ALTER TABLE AppointmentProduct
ADD quantity INT NOT NULL DEFAULT 1;

ALTER TABLE Appointment
ADD hairLengthOption INT NULL;
