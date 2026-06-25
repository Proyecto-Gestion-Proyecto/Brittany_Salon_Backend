ALTER TABLE AppointmentProduct
ADD quantity INT NOT NULL DEFAULT 1;

ALTER TABLE Appointment
ADD hairLengthOption INT NULL;