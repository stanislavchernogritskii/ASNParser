CREATE TABLE box_content (
    id SERIAL PRIMARY KEY,
    box_supplier_identifier VARCHAR(255) NOT NULL,
    box_identifier VARCHAR(255) NOT NULL,
    po_number VARCHAR(255) NOT NULL,
    isbn VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL,
    UNIQUE (box_supplier_identifier, box_identifier, po_number, isbn)
);

CREATE TABLE box_content_temp (
     id SERIAL PRIMARY KEY,
     box_supplier_identifier VARCHAR(255) NOT NULL,
     box_identifier VARCHAR(255) NOT NULL,
     po_number VARCHAR(255) NOT NULL,
     isbn VARCHAR(255) NOT NULL,
     quantity INTEGER NOT NULL
);
