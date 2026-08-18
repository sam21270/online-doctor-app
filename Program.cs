// Online Doctor Application — modern port of the original .NET Framework 4.0 web service (2022)
// (WebAPI/ServiceAPI.cs). Same endpoints, same JSON response shapes
// ({ "status":"ok", "Data":[{"data0":...}] }), backed by a local SQLite file instead of
// the original remote SQL Server. SQL is parameterized (the original concatenated strings).

using System.Text;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var root = builder.Environment.ContentRootPath;
var dbPath = Path.Combine(root, "adoc.db");
var connString = $"Data Source={dbPath}";

if (!File.Exists(dbPath))
{
    using var c = new SqliteConnection(connString);
    c.Open();
    using var init = c.CreateCommand();
    init.CommandText = File.ReadAllText(Path.Combine(root, "schema.sql"));
    init.ExecuteNonQuery();
}

// ---------------------------------------------------------------- helpers

List<string[]> Query(string sql, params (string name, string val)[] ps)
{
    using var c = new SqliteConnection(connString);
    c.Open();
    using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? "");
    using var r = cmd.ExecuteReader();
    var rows = new List<string[]>();
    while (r.Read())
    {
        var row = new string[r.FieldCount];
        for (var i = 0; i < r.FieldCount; i++)
            row[i] = r.IsDBNull(i) ? "" : r.GetValue(i).ToString() ?? "";
        rows.Add(row);
    }
    return rows;
}

void Exec(string sql, params (string name, string val)[] ps)
{
    using var c = new SqliteConnection(connString);
    c.Open();
    using var cmd = c.CreateCommand();
    cmd.CommandText = sql;
    foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? "");
    cmd.ExecuteNonQuery();
}

string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

// Same output shape as the original JSONMaker.Maker: columns become data0, data1, ...
string Maker(List<string[]> rows)
{
    var sb = new StringBuilder("{ \"status\" : \"ok\",\"Data\" :[");
    for (var i = 0; i < rows.Count; i++)
    {
        sb.Append('{');
        for (var j = 0; j < rows[i].Length; j++)
            sb.Append($" \"data{j}\" : \"{Esc(rows[i][j])}\",");
        sb.Length--;
        sb.Append("},");
    }
    if (rows.Count > 0) sb.Length--;
    sb.Append("] }");
    return sb.ToString();
}

string Single(string v) => $"{{ \"status\" : \"{Esc(v)}\" }}";
string Err(string m) => $"{{ \"status\": \"error\" , \"Data\": \"{Esc(m)}\" }}";
IResult J(string s) => Results.Text(s, "application/json; charset=utf-8");

// Same id scheme as the original getId(): max existing id + 1, else a fixed seed.
string GetId(string src)
{
    var (seed, sql) = src switch
    {
        "D"  => ("1000",   "select Did from Doctor order by CAST(Did AS INT) DESC"),
        "Di" => ("10001",  "select Id from Dise order by CAST(Id AS INT) DESC"),
        "P"  => ("100",    "select Pid from Patient order by CAST(Pid AS INT) DESC"),
        _    => ("100000", "select Aid from Appointment order by CAST(Aid AS INT) DESC"),
    };
    var rows = Query(sql);
    return rows.Count > 0 ? (int.Parse(rows[0][0]) + 1).ToString() : seed;
}

void AddTransaction(string aid, string sid, string rid, string price, string status, string date, string time) =>
    Exec("insert into ATransaction(Aid,SenderId,RecieverId,price,status,tdate,ttime) values(@a,@s,@r,@p,@st,@d,@t)",
        ("@a", aid), ("@s", sid), ("@r", rid), ("@p", price), ("@st", status), ("@d", date), ("@t", time));

void AddToNotification(string uid, string src, string title, string message, string date, string time, string user)
{
    if (src != "no")
    {
        var q = user == "doc"
            ? Query("select Name from Doctor where Did=@i", ("@i", src))
            : Query("select Name from Patient where Pid=@i", ("@i", src));
        var dname = q.Count > 0 ? q[0][0] : "";
        if (title.Contains("Message")) title = title + " " + dname;
        else message = message + " " + dname;
    }
    Exec("insert into ANotification(Uid,Src,Title,Message,ndate,ntime) values(@u,@s,@ti,@m,@d,@t)",
        ("@u", uid), ("@s", src), ("@ti", title), ("@m", message), ("@d", date), ("@t", time));
}

void AddToChat(string pid, string did)
{
    var exists = Query("select * from Chatnames where pid=@p AND did=@d", ("@p", pid), ("@d", did));
    if (exists.Count == 0)
        Exec("insert into Chatnames(pid,did,PName,DName) select @p, @d, " +
             "(select Name from Patient where Pid=@p), (select Name from Doctor where Did=@d)",
            ("@p", pid), ("@d", did));
}

int CancelCount() => Query("select * from Appointment where status='Cancelled'").Count;

var docCols = "d.Did,d.Name,d.Address,d.City,d.Cate,d.Latlng,d.Cont,d.Email," +
              "(select first from Dprice where Did=d.Did),(select rest from Dprice where Did=d.Did)," +
              "(select currency from Dprice where Did=d.Did)";

// Wrap every handler so exceptions come back as the original {"status":"error"} payload.
IResult Run(Func<string> f)
{
    try { return J(f()); }
    catch (Exception e) { return J(Err(e.Message)); }
}

// ---------------------------------------------------------------- shared / admin

app.MapGet("/demo", () => Results.Content(
    File.ReadAllText(Path.Combine(root, "demo.html")), "text/html; charset=utf-8"));

app.MapGet("/", () => Results.Text(
    "JSONWebAPI - Online Doctor Application (modern .NET port)\n" +
    "JSON API for the Android application. Endpoints live under /api/{method}.\n" +
    "Human-friendly demo UI: /demo\n" +
    "Examples:\n" +
    "  /api/ALogin?usern=admin&pass=admin123\n" +
    "  /api/getDoctors\n" +
    "  /api/PLogin?email=ravi@test.com&pass=1234\n" +
    "  /api/sysone?sys=fever   -> /api/systwo?sys=headache   -> /api/final1?ID=100&Date=2026-07-10\n",
    "text/plain; charset=utf-8"));

app.MapGet("/api/getNotification", (string uid) => Run(() =>
{
    var rows = Query("select * from ANotification where Uid=@u order by Nid DESC limit 1", ("@u", uid));
    if (rows.Count == 0) return Single("no");
    var ans = Maker(rows);
    Exec("delete from ANotification where Nid=@n", ("@n", rows[0][0]));
    return ans;
}));

app.MapGet("/api/ALogin", (string usern, string pass) => Run(() =>
    Query("select * from Admin where Username=@u AND Pass=@p", ("@u", usern), ("@p", pass)).Count > 0
        ? Single("true") : Single("false")));

app.MapGet("/api/getDoctors", () => Run(() =>
{
    var rows = Query($"select {docCols} from Doctor d order by d.Name");
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/getAllDocs", () => Run(() =>
{
    var rows = Query($"select {docCols} from Doctor d order by d.Name");
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/AddDoc", (string Name, string Address, string city, string Cate, string Latlng, string Cont, string email, string first, string second) => Run(() =>
{
    var id = GetId("D");
    if (Query("select * from Doctor where Email=@e", ("@e", email)).Count > 0) return Single("already");
    Exec("insert into Doctor values(@i,@n,@a,@c,@ca,@l,@co,@e,@pw)",
        ("@i", id), ("@n", Name), ("@a", Address), ("@c", city), ("@ca", Cate),
        ("@l", Latlng), ("@co", Cont), ("@e", email), ("@pw", Cont)); // original used Cont as initial password
    Exec("insert into Dprice values(@i,@f,@s,'R')", ("@i", id), ("@f", first), ("@s", second));
    return Single("true");
}));

app.MapGet("/api/UpdateDoc", (string did, string Name, string Address, string city, string Cate, string Latlng, string Cont, string email, string first, string second) => Run(() =>
{
    if (Query("select * from Doctor where Email=@e AND Did<>@d", ("@e", email), ("@d", did)).Count > 0) return Single("already");
    Exec("update Doctor set Name=@n,Address=@a,City=@c,Cate=@ca,Latlng=@l,Cont=@co,Email=@e where Did=@d",
        ("@n", Name), ("@a", Address), ("@c", city), ("@ca", Cate), ("@l", Latlng), ("@co", Cont), ("@e", email), ("@d", did));
    Exec("update Dprice set first=@f,rest=@s where Did=@d", ("@f", first), ("@s", second), ("@d", did));
    return Single("true");
}));

app.MapGet("/api/DelDoc", (string did) => Run(() =>
{
    Exec("delete from Doctor where Did=@d", ("@d", did));
    Exec("delete from Appointment where Did=@d", ("@d", did));
    Exec("delete from Chatnames where did=@d", ("@d", did));
    Exec("delete from Chats where SenderId=@d", ("@d", did));
    Exec("delete from Chats where RecId=@d", ("@d", did));
    Exec("delete from Dprice where Did=@d", ("@d", did));
    return Single("true");
}));

app.MapGet("/api/getDiseaselist", () => Run(() =>
{
    var rows = Query("select Id,DName,Sym,Type from Dise order by DName");
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/AddDisease", (string Name, string sym, string type) => Run(() =>
{
    var id = GetId("Di");
    if (Query("select * from Dise where DName=@n AND type=@t", ("@n", Name), ("@t", type)).Count > 0) return Single("already");
    Exec("insert into Dise values(@i,@n,@s,@t,'0')", ("@i", id), ("@n", Name), ("@s", sym), ("@t", type));
    return Single("true");
}));

app.MapGet("/api/UpdateDisease", (string id, string Name, string sym, string type) => Run(() =>
{
    if (Query("select * from Dise where DName=@n AND type=@t AND Id<>@i", ("@n", Name), ("@t", type), ("@i", id)).Count > 0) return Single("already");
    Exec("update Dise set DName=@n,Sym=@s,Type=@t where Id=@i", ("@n", Name), ("@s", sym), ("@t", type), ("@i", id));
    return Single("true");
}));

app.MapGet("/api/DelDisease", (string id) => Run(() =>
{
    Exec("delete from Dise where Id=@i", ("@i", id));
    return Single("true");
}));

app.MapGet("/api/getPatients", () => Run(() =>
{
    var rows = Query("select Pid,Pic,Name,Gender,DOB,Address,City,State,Cont,Email from Patient order by Name");
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/getFeedback", () => Run(() =>
{
    var rows = Query("select (select Name from Patient where Pid=f.Uid),(select Name from Doctor where Did=f.did),f.src,f.feed,f.fdate,f.ftime from Feedback f order by f.id DESC");
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

// ---------------------------------------------------------------- patient

app.MapGet("/api/Register", (string pic, string name, string gender, string dob, string address, string city, string state, string cont, string email, string pass) => Run(() =>
{
    var id = GetId("P");
    if (Query("select * from Patient where Email=@e", ("@e", email)).Count > 0) return Single("already");
    Exec("insert into Patient values(@i,@pi,@n,@g,@d,@a,@c,@s,@co,@e,@p)",
        ("@i", id), ("@pi", pic), ("@n", name), ("@g", gender), ("@d", dob),
        ("@a", address), ("@c", city), ("@s", state), ("@co", cont), ("@e", email), ("@p", pass));
    return Single("true");
}));

app.MapGet("/api/PLogin", (string email, string pass) => Run(() =>
{
    var rows = Query("select Pid from Patient where Email=@e AND Pass=@p", ("@e", email), ("@p", pass));
    return rows.Count > 0 ? Maker(rows) : Single("false");
}));

app.MapGet("/api/PgetProfile", (string pid) => Run(() =>
{
    var rows = Query("select Pid,Pic,Name,Gender,DOB,Address,City,State,Cont,Email from Patient where Pid=@p", ("@p", pid));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/PUpdateProfile", (string pid, string pic, string name, string gender, string dob, string address, string city, string state, string cont, string email) => Run(() =>
{
    if (Query("select * from Patient where Email=@e AND pid<>@p", ("@e", email), ("@p", pid)).Count > 0) return Single("already");
    Exec("update Patient set Pic=@pi,Name=@n,Gender=@g,DOB=@d,Address=@a,City=@c,State=@s,Cont=@co,Email=@e where Pid=@p",
        ("@pi", pic), ("@n", name), ("@g", gender), ("@d", dob), ("@a", address),
        ("@c", city), ("@s", state), ("@co", cont), ("@e", email), ("@p", pid));
    return Single("true");
}));

app.MapGet("/api/PChangePass", (string pid, string oldpass, string newpass) => Run(() =>
{
    if (Query("select Pass from Patient where pid=@p AND Pass=@o", ("@p", pid), ("@o", oldpass)).Count == 0) return Single("false");
    Exec("update Patient set Pass=@n where Pid=@p", ("@n", newpass), ("@p", pid));
    return Single("true");
}));

// src: current / previous / pending / others
app.MapGet("/api/PgetAppointment", (string pid, string src, string date) => Run(() =>
{
    var sel = "select a.Aid,a.Did,(select Name from Doctor where Did=a.Did),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Pid=@p";
    var rows = src switch
    {
        "current"  => Query(sel + " AND a.status='Confirmed' AND a.adate>=@d order by a.adate,a.atime", ("@p", pid), ("@d", date)),
        "previous" => Query(sel + " AND a.status='Confirmed' AND a.adate<@d order by a.adate DESC,a.atime DESC", ("@p", pid), ("@d", date)),
        "pending"  => Query(sel + " AND a.status='Pending' order by a.adate,a.atime", ("@p", pid)),
        _          => Query(sel + " AND a.status<>'Pending' AND a.status<>'Confirmed' order by a.adate,a.atime", ("@p", pid)),
    };
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/PgetPriceInfo", (string did, string pid) => Run(() =>
    Query("select * from Appointment where Did=@d AND Pid=@p AND status='Confirmed'", ("@d", did), ("@p", pid)).Count > 0
        ? Single("first") : Single("rest")));

app.MapGet("/api/PaddAppointment", (string did, string pid, string note, string adate, string atime, string price, string date, string time) => Run(() =>
{
    var id = GetId("A");
    if (Query("select * from Appointment where Did=@d AND Pid=@p AND adate=@ad AND atime=@at",
            ("@d", did), ("@p", pid), ("@ad", adate), ("@at", atime)).Count > 0) return Single("already");
    Exec("insert into Appointment values(@i,@d,@p,@n,@pr,@ad,@at,'Pending')",
        ("@i", id), ("@d", did), ("@p", pid), ("@n", note), ("@pr", price), ("@ad", adate), ("@at", atime));
    AddTransaction(id, pid, did, price, "Appointment Booked", date, time);
    AddToNotification(did, pid, "New Appointment", "Appointment Booked by", date, time, "pat");
    return Single("true");
}));

app.MapGet("/api/PcancelAppointment", (string aid, string did, string pid, string price, string date, string time) => Run(() =>
{
    if (CancelCount() > 10) return Single("false");
    Exec("update Appointment set status='Cancelled' where aid=@a", ("@a", aid));
    AddTransaction(aid, did, pid, price, "Appointment Cancelled", date, time);
    AddToNotification(did, pid, "Appointment Cancelled", "Appointment Cancelled by", date, time, "pat");
    return Single("true");
}));

app.MapGet("/api/PgetChats_Names", (string pid) => Run(() =>
{
    var rows = Query("select did,DName from Chatnames where pid=@p", ("@p", pid));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/PgetChatsbyId", (string pid, string did) => Run(() =>
{
    var rows = Query("select SenderId,RecId,Message,cdate,ctime from Chats where (SenderId=@p AND RecId=@d) OR (SenderId=@d AND RecId=@p) order by cid",
        ("@p", pid), ("@d", did));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

// src: pat / doc
app.MapGet("/api/AddChat", (string sender, string rec, string message, string date, string time, string src) => Run(() =>
{
    Exec("insert into Chats(SenderId,RecId,Message,Extra,cdate,ctime) values(@s,@r,@m,'',@d,@t)",
        ("@s", sender), ("@r", rec), ("@m", message), ("@d", date), ("@t", time));
    if (src == "pat")
    {
        AddToChat(sender, rec);
        AddToNotification(rec, sender, "Message from", message, date, time, "pat");
    }
    else
    {
        AddToChat(rec, sender);
        AddToNotification(rec, sender, "Message from", message, date, time, "doc");
    }
    return Single("true");
}));

app.MapGet("/api/PgetFeedback", (string pid) => Run(() =>
{
    var rows = Query("select (select Name from Doctor where Did=f.did),f.src,f.feed,f.fdate,f.ftime from Feedback f where f.Uid=@p order by f.id DESC", ("@p", pid));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/PAddFeedback", (string uid, string did, string src, string feed, string date, string time) => Run(() =>
{
    Exec("insert into Feedback(Uid,did,src,feed,fdate,ftime) values(@u,@d,@s,@f,@da,@t)",
        ("@u", uid), ("@d", did), ("@s", src), ("@f", feed), ("@da", date), ("@t", time));
    if (src != "Admin") AddToNotification(did, "no", "New Feedback", feed, date, time, "pat");
    return Single("true");
}));

// src: debit / credit
app.MapGet("/api/PgetTransactions", (string pid, string src) => Run(() =>
{
    var rows = src == "debit"
        ? Query("select t.Aid,(select Name from Doctor where Did=t.RecieverId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.SenderId=@p order by t.Tid DESC", ("@p", pid))
        : Query("select t.Aid,(select Name from Doctor where Did=t.SenderId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.RecieverId=@p order by t.Tid DESC", ("@p", pid));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

// ---------------------------------------------------------------- symptom checker
// sysone(first symptom) -> systwo(next symptom, repeat) -> final1(diagnosis + doctor suggestions)

// Port of getSym(): for each flagged disease, queue its first symptom not already asked about.
void CollectKeywords()
{
    foreach (var d in Query("select Sym from Dise where Flag='1'"))
    {
        foreach (var tok in d[0].Split(','))
        {
            if (Query("select * from Final where Sym like @p", ("@p", $"%{tok.ToLower()}%")).Count == 0)
            {
                Exec("insert into Keyword(Sym) values(@s)", ("@s", tok));
                break; // original moved on to the next disease after one insert
            }
        }
    }
}

app.MapGet("/api/sysone", (string sys) => Run(() =>
{
    Exec("delete from Final");
    Exec("delete from Keyword");
    Exec("update Dise set Flag='0'");
    if (Query("select DName from Dise where Sym like @p", ("@p", $"%{sys}%")).Count == 0) return Single("no");
    Exec("update Dise set Flag='1' where Sym like @p", ("@p", $"%{sys}%"));
    Exec("insert into Final(Sym) values(@s)", ("@s", sys.ToLower()));
    CollectKeywords();
    var ks = Query("select distinct Sym from Keyword");
    return ks.Count > 0 ? Maker(ks) : Single("no");
}));

app.MapGet("/api/systwo", (string sys) =>
{
    try
    {
        Exec("delete from Keyword");
        Exec("update Dise set Flag='0'");
        if (Query("select DName from Dise where Sym like @p", ("@p", $"%{sys}%")).Count == 0) return J(Single("no"));
        Exec("insert into Final(Sym) values(@s)", ("@s", sys.ToLower()));

        var fins = Query("select Sym from Final");
        var conds = string.Join(" And ", fins.Select((_, i) => $"Sym LIKE @f{i}"));
        var ps = fins.Select((f, i) => ($"@f{i}", $"%{f[0]}%")).ToArray();
        Exec("update Dise set Flag='1' where " + conds, ps);

        CollectKeywords();
        var ks = Query("select Sym from Keyword");
        return J(ks.Count > 0 ? Maker(ks) : Single("no"));
    }
    catch
    {
        return J(Single("no")); // original swallowed errors here as "no"
    }
});

app.MapGet("/api/final1", (string ID, string Date) => Run(() =>
{
    var fins = Query("select Sym from Final");
    var conds = string.Join(" And ", fins.Select((_, i) => $"Sym LIKE @f{i}"));
    var ps = fins.Select((f, i) => ($"@f{i}", $"%{f[0]}%")).ToArray();
    var matches = Query("select DName,Type from Dise where " + conds, ps);
    if (matches.Count == 0) return Single("no");

    var diseases = string.Join("*", matches.Select(r => r[0]));
    var type = matches[0][1];
    var syms = string.Join(",", fins.Select(f => f[0])) + ",";

    Exec("insert into history(UId,Sym,Disease,type,Date) values(@u,@s,@d,@t,@dt)",
        ("@u", ID), ("@s", syms), ("@d", string.Concat(matches.Select(r => r[0]))), ("@t", type),
        ("@dt", DateTime.Now.ToString()));

    var docs = Query($"select {docCols} from Doctor d where d.Cate like @c", ("@c", $"%{type}%"));
    var docStr = docs.Count == 0 ? "no" : string.Concat(docs.Select(r => string.Join("*", r) + "#"));

    // data0: diseases (*-separated), data1: doctor category, data2: doctors (*-fields, #-rows) or "no"
    return Maker(new List<string[]> { new[] { diseases, type, docStr } });
}));

// ---------------------------------------------------------------- doctor

app.MapGet("/api/DLogin", (string email, string pass) => Run(() =>
{
    var rows = Query("select Did from Doctor where Email=@e AND Pass=@p", ("@e", email), ("@p", pass));
    return rows.Count > 0 ? Maker(rows) : Single("false");
}));

app.MapGet("/api/DgetProfile", (string did) => Run(() =>
{
    var rows = Query($"select {docCols} from Doctor d where d.Did=@d", ("@d", did));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/DUpdateProfile", (string did, string name, string address, string city, string cate, string latlng, string cont, string email, string first, string rest) => Run(() =>
{
    if (Query("select * from Doctor where Email=@e AND Did<>@d", ("@e", email), ("@d", did)).Count > 0) return Single("already");
    Exec("update Doctor set Name=@n,Address=@a,City=@c,Cate=@ca,Latlng=@l,Cont=@co,Email=@e where Did=@d",
        ("@n", name), ("@a", address), ("@c", city), ("@ca", cate), ("@l", latlng), ("@co", cont), ("@e", email), ("@d", did));
    Exec("update Dprice set first=@f,rest=@r where Did=@d", ("@f", first), ("@r", rest), ("@d", did));
    return Single("true");
}));

app.MapGet("/api/DChangePass", (string did, string oldpass, string newpass) => Run(() =>
{
    if (Query("select Pass from Doctor where Did=@d AND Pass=@o", ("@d", did), ("@o", oldpass)).Count == 0) return Single("false");
    Exec("update Doctor set Pass=@n where Did=@d", ("@n", newpass), ("@d", did));
    return Single("true");
}));

app.MapGet("/api/DgetChats_Names", (string did) => Run(() =>
{
    var rows = Query("select pid,PName from Chatnames where did=@d", ("@d", did));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/DgetChatsbyId", (string pid, string did) => Run(() =>
{
    var rows = Query("select SenderId,RecId,Message,cdate,ctime from Chats where (SenderId=@d AND RecId=@p) OR (SenderId=@p AND RecId=@d) order by cid",
        ("@p", pid), ("@d", did));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

// src: current / previous / pending / others
app.MapGet("/api/DgetAppointment", (string did, string src, string date) => Run(() =>
{
    var sel = "select a.Aid,a.Pid,(select Name from Patient where Pid=a.Pid),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Did=@d";
    var rows = src switch
    {
        "current"  => Query(sel + " AND a.status='Confirmed' AND a.adate>=@dt order by a.adate,a.atime", ("@d", did), ("@dt", date)),
        "previous" => Query(sel + " AND a.status='Confirmed' AND a.adate<@dt order by a.adate DESC,a.atime DESC", ("@d", did), ("@dt", date)),
        "pending"  => Query(sel + " AND a.status='Pending' order by a.adate,a.atime", ("@d", did)),
        _          => Query(sel + " AND a.status<>'Pending' AND a.status<>'Confirmed' order by a.adate,a.atime", ("@d", did)),
    };
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

// status: Confirmed / Rejected
app.MapGet("/api/DChangeStatus", (string aid, string did, string pid, string price, string date, string time, string status) => Run(() =>
{
    Exec("update Appointment set status=@s where aid=@a", ("@s", status), ("@a", aid));
    if (status == "Rejected") AddTransaction(aid, did, pid, price, "Appointment Rejected", date, time);
    AddToNotification(pid, did, "Appointment " + status, "Appointment is " + status + " by", date, time, "doc");
    return Single("true");
}));

// src: debit / credit
app.MapGet("/api/DgetTransactions", (string did, string src) => Run(() =>
{
    var rows = src == "debit"
        ? Query("select t.Aid,(select Name from Patient where Pid=t.RecieverId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.SenderId=@d order by t.Tid DESC", ("@d", did))
        : Query("select t.Aid,(select Name from Patient where Pid=t.SenderId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.RecieverId=@d order by t.Tid DESC", ("@d", did));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.MapGet("/api/DgetFeedback", (string did) => Run(() =>
{
    var rows = Query("select (select Name from Patient where Pid=f.Uid),f.feed,f.fdate,f.ftime from Feedback f where f.did=@d order by f.id DESC", ("@d", did));
    return rows.Count > 0 ? Maker(rows) : Single("no");
}));

app.Run();
