-- ============================================================
-- BRITTANY SALON - SCRIPT POSTGRESQL PARA SUPABASE
-- Version: TEST (transaccional, no persiste cambios)
--
-- ESTRATEGIA: dos fases
--   1) CREATE TABLE sin FKs inline (evita pre-parse de FKs)
--   2) ALTER TABLE para agregar FKs despues de que existan las tablas
--
-- USO:
--   1) Pegar en Supabase SQL Editor y Run
--   2) Revisar los SELECT de verificacion
--   3) ROLLBACK automatico al final
--   4) Si todo esta bien, ejecutar db.sql (produccion)
-- ============================================================

BEGIN;

-- ============================================================
-- LIMPIEZA
-- ============================================================
DROP TABLE IF EXISTS "RefreshToken" CASCADE;
DROP TABLE IF EXISTS "Payment" CASCADE;
DROP TABLE IF EXISTS "AppointmentProduct" CASCADE;
DROP TABLE IF EXISTS "AppointmentService" CASCADE;
DROP TABLE IF EXISTS "Appointment" CASCADE;
DROP TABLE IF EXISTS "EmployeeService" CASCADE;
DROP TABLE IF EXISTS "Review" CASCADE;
DROP TABLE IF EXISTS "Inventory" CASCADE;
DROP TABLE IF EXISTS "Product" CASCADE;
DROP TABLE IF EXISTS "Service" CASCADE;
DROP TABLE IF EXISTS "Client" CASCADE;
DROP TABLE IF EXISTS "Employee" CASCADE;
DROP TABLE IF EXISTS "Category" CASCADE;

-- ============================================================
-- FASE 1: CREAR TABLAS SIN FOREIGN KEYS
-- Solo columnas, PK, defaults, CHECK e UNIQUE inline.
-- ============================================================

CREATE TABLE "Category" (
    "categoryId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "categoryName" VARCHAR(50) NOT NULL UNIQUE,
    "categoryDescription" VARCHAR(255),
    "isActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE "Employee" (
    "employeeId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name" VARCHAR(100) NOT NULL,
    "email" VARCHAR(150) NOT NULL UNIQUE,
    "phone" VARCHAR(20),
    "passwordHash" VARCHAR(255) NOT NULL,
    "specialty" VARCHAR(100),
    "imageUrl" VARCHAR(255),
    "isActive" BOOLEAN DEFAULT TRUE,
    "createdAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "Client" (
    "clientId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name" VARCHAR(100) NOT NULL,
    "email" VARCHAR(150) NOT NULL UNIQUE,
    "phone" VARCHAR(20),
    "passwordHash" VARCHAR(255) NOT NULL,
    "pendingBalance" DECIMAL(10,2) DEFAULT 0,
    "imageUrl" VARCHAR(255),
    "isActive" BOOLEAN DEFAULT TRUE,
    "createdAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "Service" (
    "serviceId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "serviceName" VARCHAR(100) NOT NULL,
    "serviceDescription" VARCHAR(255),
    "price" DECIMAL(10,2) NOT NULL,
    "durationMinutes" INTEGER NOT NULL,
    "imageUrl" VARCHAR(255),
    "serviceType" VARCHAR(50),
    "isActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE "Product" (
    "productId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "productName" VARCHAR(100) NOT NULL,
    "productDescription" VARCHAR(255),
    "price" DECIMAL(10,2) NOT NULL,
    "imageUrl" VARCHAR(255),
    "expirationDate" DATE,
    "isActive" BOOLEAN DEFAULT TRUE,
    "categoryId" INTEGER NOT NULL
);

CREATE TABLE "Inventory" (
    "inventoryId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "productId" INTEGER NOT NULL UNIQUE,
    "quantity" INTEGER NOT NULL,
    "minimumStock" INTEGER NOT NULL,
    "maximumStock" INTEGER NOT NULL,
    "location" VARCHAR(100),
    "notes" VARCHAR(255),
    "lastUpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "isActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE "Review" (
    "reviewId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "comment" VARCHAR(255),
    "rating" INTEGER CHECK ("rating" BETWEEN 1 AND 5),
    "imageUrl" VARCHAR(255),
    "response" VARCHAR(255),
    "reviewDate" DATE DEFAULT CURRENT_DATE,
    "clientId" INTEGER NOT NULL,
    "employeeId" INTEGER NULL
);

CREATE TABLE "EmployeeService" (
    "employeeServiceId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "employeeId" INTEGER NOT NULL,
    "serviceId" INTEGER NOT NULL
);

CREATE TABLE "Appointment" (
    "appointmentId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "appointmentDate" DATE NOT NULL,
    "startTime" TIMESTAMP NOT NULL,
    "endTime" TIMESTAMP NOT NULL,
    "appointmentStatus" VARCHAR(50),
    "totalCost" DECIMAL(10,2),
    "isActive" BOOLEAN DEFAULT TRUE,
    "clientId" INTEGER NOT NULL,
    "hairLengthOption" INTEGER
);

CREATE TABLE "AppointmentService" (
    "appointmentServiceId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "appointmentId" INTEGER NOT NULL,
    "serviceId" INTEGER NOT NULL,
    "servicePrice" DECIMAL(10,2)
);

CREATE TABLE "AppointmentProduct" (
    "appointmentProductId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "appointmentId" INTEGER NOT NULL,
    "productId" INTEGER NOT NULL,
    "quantity" INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE "Payment" (
    "paymentId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "appointmentId" INTEGER NOT NULL,
    "amount" DECIMAL(10,2) NOT NULL,
    "paymentStatus" VARCHAR(50),
    "paymentDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "paymentMethod" VARCHAR(50),
    "isActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE "RefreshToken" (
    "refreshTokenId" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "token" VARCHAR(500) NOT NULL,
    "userId" INTEGER NOT NULL,
    "userType" VARCHAR(50) NOT NULL,
    "expirationDate" TIMESTAMP NOT NULL,
    "isRevoked" BOOLEAN DEFAULT FALSE,
    "createdAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "revokedAt" TIMESTAMP NULL,
    "revocationReason" VARCHAR(255)
);

-- ============================================================
-- FASE 2: AGREGAR FOREIGN KEYS
-- En ALTER TABLE separados para evitar pre-parse de FKs.
-- ============================================================

ALTER TABLE "Product"
    ADD CONSTRAINT "FK_Product_Category"
    FOREIGN KEY ("categoryId") REFERENCES "Category"("categoryId");

ALTER TABLE "Inventory"
    ADD CONSTRAINT "FK_Inventory_Product"
    FOREIGN KEY ("productId") REFERENCES "Product"("productId");

ALTER TABLE "Review"
    ADD CONSTRAINT "FK_Review_Client"
    FOREIGN KEY ("clientId") REFERENCES "Client"("clientId");

ALTER TABLE "Review"
    ADD CONSTRAINT "FK_Review_Employee"
    FOREIGN KEY ("employeeId") REFERENCES "Employee"("employeeId");

ALTER TABLE "EmployeeService"
    ADD CONSTRAINT "FK_EmployeeService_Employee"
    FOREIGN KEY ("employeeId") REFERENCES "Employee"("employeeId");

ALTER TABLE "EmployeeService"
    ADD CONSTRAINT "FK_EmployeeService_Service"
    FOREIGN KEY ("serviceId") REFERENCES "Service"("serviceId");

ALTER TABLE "Appointment"
    ADD CONSTRAINT "FK_Appointment_Client"
    FOREIGN KEY ("clientId") REFERENCES "Client"("clientId");

ALTER TABLE "AppointmentService"
    ADD CONSTRAINT "FK_AppointmentService_Appointment"
    FOREIGN KEY ("appointmentId") REFERENCES "Appointment"("appointmentId");

ALTER TABLE "AppointmentService"
    ADD CONSTRAINT "FK_AppointmentService_Service"
    FOREIGN KEY ("serviceId") REFERENCES "Service"("serviceId");

ALTER TABLE "AppointmentProduct"
    ADD CONSTRAINT "FK_AppointmentProduct_Appointment"
    FOREIGN KEY ("appointmentId") REFERENCES "Appointment"("appointmentId");

ALTER TABLE "AppointmentProduct"
    ADD CONSTRAINT "FK_AppointmentProduct_Product"
    FOREIGN KEY ("productId") REFERENCES "Product"("productId");

ALTER TABLE "AppointmentProduct"
    ADD CONSTRAINT "UQ_AppointmentProduct_AppointmentId_ProductId"
    UNIQUE ("appointmentId", "productId");

ALTER TABLE "Payment"
    ADD CONSTRAINT "FK_Payment_Appointment"
    FOREIGN KEY ("appointmentId") REFERENCES "Appointment"("appointmentId");

-- ============================================================
-- INDICES DE REFRESH TOKEN
-- ============================================================

CREATE UNIQUE INDEX "IX_RefreshToken_Token" ON "RefreshToken" ("token");
CREATE INDEX "IX_RefreshToken_UserId_UserType" ON "RefreshToken" ("userId", "userType");

-- ============================================================
-- DATOS INICIALES
-- ============================================================

INSERT INTO "Category" ("categoryName", "categoryDescription")
VALUES ('General', 'Categoria por defecto');

-- ============================================================
-- VERIFICACION
-- ============================================================

SELECT 'Tablas creadas' AS verificacion, count(*) AS total
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN (
    'Category','Employee','Client','Service','Product','Inventory',
    'Review','EmployeeService','Appointment','AppointmentService',
    'AppointmentProduct','Payment','RefreshToken'
  );

SELECT 'Foreign Keys' AS verificacion, count(*) AS total
FROM information_schema.table_constraints
WHERE constraint_type = 'FOREIGN KEY'
  AND table_schema = 'public';

SELECT 'Unique constraints' AS verificacion, count(*) AS total
FROM information_schema.table_constraints
WHERE constraint_type = 'UNIQUE'
  AND table_schema = 'public';

SELECT * FROM "Category";

-- ============================================================
-- ROLLBACK: descarta todo
-- ============================================================
ROLLBACK;
