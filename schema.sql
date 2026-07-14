-- Schema reconstructed from the original ServiceAPI.cs SQL queries (SQL Server -> SQLite)

CREATE TABLE Admin (
    Username TEXT,
    Pass     TEXT
);

CREATE TABLE Doctor (
    Did     TEXT,
    Name    TEXT,
    Address TEXT,
    City    TEXT,
    Cate    TEXT,
    Latlng  TEXT,
    Cont    TEXT,
    Email   TEXT,
    Pass    TEXT
);

CREATE TABLE Dprice (
    Did      TEXT,
    first    TEXT,
    rest     TEXT,
    currency TEXT
);

CREATE TABLE Dise (
    Id    TEXT,
    DName TEXT,
    Sym   TEXT,
    Type  TEXT,
    Flag  TEXT
);

CREATE TABLE Patient (
    Pid     TEXT,
    Pic     TEXT,
    Name    TEXT,
    Gender  TEXT,
    DOB     TEXT,
    Address TEXT,
    City    TEXT,
    State   TEXT,
    Cont    TEXT,
    Email   TEXT,
    Pass    TEXT
);

CREATE TABLE Appointment (
    Aid    TEXT,
    Did    TEXT,
    Pid    TEXT,
    note   TEXT,
    price  TEXT,
    adate  TEXT,
    atime  TEXT,
    status TEXT
);

CREATE TABLE ATransaction (
    Tid        INTEGER PRIMARY KEY AUTOINCREMENT,
    Aid        TEXT,
    SenderId   TEXT,
    RecieverId TEXT,
    price      TEXT,
    status     TEXT,
    tdate      TEXT,
    ttime      TEXT
);

CREATE TABLE ANotification (
    Nid     INTEGER PRIMARY KEY AUTOINCREMENT,
    Uid     TEXT,
    Src     TEXT,
    Title   TEXT,
    Message TEXT,
    ndate   TEXT,
    ntime   TEXT
);

CREATE TABLE Chatnames (
    pid   TEXT,
    did   TEXT,
    PName TEXT,
    DName TEXT
);

CREATE TABLE Chats (
    cid      INTEGER PRIMARY KEY AUTOINCREMENT,
    SenderId TEXT,
    RecId    TEXT,
    Message  TEXT,
    Extra    TEXT,
    cdate    TEXT,
    ctime    TEXT
);

CREATE TABLE Feedback (
    id    INTEGER PRIMARY KEY AUTOINCREMENT,
    Uid   TEXT,
    did   TEXT,
    src   TEXT,
    feed  TEXT,
    fdate TEXT,
    ftime TEXT
);

-- scratch tables used by the symptom-checker (sysone/systwo/final1)
CREATE TABLE Final   (Sym TEXT);
CREATE TABLE Keyword (Sym TEXT);

CREATE TABLE history (
    HId     INTEGER PRIMARY KEY AUTOINCREMENT,
    UId     TEXT,
    Sym     TEXT,
    Disease TEXT,
    type    TEXT,
    Date    TEXT
);

-- ------------------------------------------------------------------
-- Seed data so the API is demoable out of the box
-- ------------------------------------------------------------------

INSERT INTO Admin VALUES ('admin', 'admin123');

INSERT INTO Doctor VALUES
 ('1000', 'Dr. Asha Mehta',  'MG Road 12',      'Mumbai',    'General Physician', '19.0760,72.8777', '9876543210', 'asha@adoc.com',  'doc123'),
 ('1001', 'Dr. Rohan Verma', 'CP Block A',      'Delhi',     'Cardiologist',      '28.6139,77.2090', '9876500001', 'rohan@adoc.com', 'doc123'),
 ('1002', 'Dr. Priya Nair',  'Indiranagar 100', 'Bengaluru', 'Pulmonologist',     '12.9716,77.5946', '9876500002', 'priya@adoc.com', 'doc123');

INSERT INTO Dprice VALUES
 ('1000', '500', '300', 'R'),
 ('1001', '800', '500', 'R'),
 ('1002', '600', '400', 'R');

INSERT INTO Dise VALUES
 ('10001', 'Common Cold',  'cough,sneezing,runny nose,sore throat',   'General Physician', '0'),
 ('10002', 'Influenza',    'fever,cough,headache,body ache',          'General Physician', '0'),
 ('10003', 'Dengue',       'fever,headache,joint pain,rash',          'General Physician', '0'),
 ('10004', 'Hypertension', 'headache,dizziness,chest pain',           'Cardiologist',      '0'),
 ('10005', 'Asthma',       'cough,wheezing,shortness of breath',      'Pulmonologist',     '0');

INSERT INTO Patient VALUES
 ('100', 'no', 'Ravi Kumar',  'Male',   '1998-04-12', 'Sector 5', 'Pune',   'MH', '9000000001', 'ravi@test.com',  '1234'),
 ('101', 'no', 'Sneha Patil', 'Female', '1999-09-30', 'Camp Rd',  'Nagpur', 'MH', '9000000002', 'sneha@test.com', '1234');

INSERT INTO Appointment VALUES
 ('100000', '1000', '100', 'Fever since 2 days', '500', '2026-07-15', '10:30 AM', 'Pending');

INSERT INTO ATransaction (Aid, SenderId, RecieverId, price, status, tdate, ttime) VALUES
 ('100000', '100', '1000', '500', 'Appointment Booked', '2026-07-10', '09:00 AM');
