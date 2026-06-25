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
