using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace JSONWebAPI
{
    public class ServiceAPI : IServiceAPI
    {
        List<string> ilist;
        SqlConnection con;
        SqlCommand cmd;
        SqlDataReader dr;
        JSONMaker jm = new JSONMaker();

        public ServiceAPI()
        {
            con = DBConnect.getConnection();
        }

        public string getId(string src)
        {
            string ans = "";
            SqlDataAdapter da = null;

            if (src == "D")
            {
                ans = "1000";
                da = new SqlDataAdapter("select DId from Doctor order by CAST(DId AS INT) DESC", con);
            }
            else if (src == "Di")
            {
                ans = "10001";
                da = new SqlDataAdapter("select Id from Dise order by CAST(Id AS INT) DESC", con);
            }
            else if (src == "P")
            {
                ans = "100";
                da = new SqlDataAdapter("select Pid from Patient order by CAST(Pid AS INT) DESC", con);
            }
            else if (src == "A")
            {
                ans = "100000";
                da = new SqlDataAdapter("select Aid from Appointment order by CAST(Aid AS INT) DESC", con);
            }

            DataSet ds = new DataSet();
            da.Fill(ds);
            int count = ds.Tables[0].Rows.Count;

            if (count > 0)
            {
                int i = Int32.Parse(ds.Tables[0].Rows[0][0].ToString()) + 1;
                ans = i.ToString();
            }

            return ans;

        }

        public string AddTransaction(string aid, string sid, string rid, string price, string status, string date, string time)
        {
            string ans = "";
            try
            {
                cmd = new SqlCommand("insert into ATransaction values('" + aid + "','" + sid + "','" + rid + "','" + price + "','" + status + "','" + date + "','" + time + "')", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                ans = jm.Singlevalue("true");
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string getcancelCount(string pid)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Appointment where status='Cancelled'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                ans = ds.Tables[0].Rows.Count.ToString();
            }
            catch (Exception e)
            {
                ans = e.Message;
            }
            return ans;
        }

        public string addToChat(string pid, string did)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Chatnames where pid='" + pid + "' AND did='" + did + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int count = ds.Tables[0].Rows.Count;
                if (count == 0)
                {
                    string ps = "(select Name from patient where Pid='" + pid + "')";
                    string ds1 = "(select Name from Doctor where Did='" + did + "')";

                    string command = "insert into Chatnames values('" + pid + "','" + did + "'," + ps + "," + ds1 + ")";
                    cmd = new SqlCommand(command, con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    ans = "true";
                }
            }
            catch (Exception e)
            {
                ans = e.Message;
            }
            return ans;
        }

        public string AddtoNotification(string uid, string src, string title, string message, string date, string time, string user)
        {
            string ans = "";
            string dname = "";
            try
            {
                if (src != "no")
                {
                    if (user == "doc")
                    {
                        SqlDataAdapter da = new SqlDataAdapter("select Name from Doctor where Did='" + src + "'", con);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dname = ds.Tables[0].Rows[0][0].ToString();
                    }

                    if (user == "pat")
                    {
                        SqlDataAdapter da = new SqlDataAdapter("select Name from Patient where Pid='" + src + "'", con);
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        dname = ds.Tables[0].Rows[0][0].ToString();
                    }

                    if (title.Contains("Message"))
                    {
                        title = title + " " + dname;
                    }
                    else
                    {
                        message = message + " " + dname;
                    }

                }
                

                cmd = new SqlCommand("insert into ANotification values('" + uid + "','" + src + "','" + title + "','" + message + "','" + date + "','" + time + "')", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            catch (Exception e)
            {
                ans = e.Message;
            }
            return ans;
        }

        public string getNotification(string uid)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Top 1 * from ANotification where Uid='" + uid + "' order by Nid DESC", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int count = ds.Tables[0].Rows.Count;
                if (count > 0)
                {
                    string nid = ds.Tables[0].Rows[0][0].ToString();
                    ans = jm.Maker(ds);

                    cmd = new SqlCommand("delete from ANotification where Nid='" + nid + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
                else
                {
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        //********************************
        //Admin
        //*******************************

        public string ALogin(string usern, string pass)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Admin where Username='" + usern + "' AND Pass='" + pass + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int count = ds.Tables[0].Rows.Count;
                if (count > 0)
                {
                    //true
                    ans = jm.Singlevalue("true");
                }
                else
                {
                    //false
                    ans = jm.Singlevalue("false");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;

        }

        public string getDoctors()
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select d.Did,d.Name,d.Address,d.City,d.Cate,d.Latlng,d.Cont,d.Email,(select first from Dprice where Did=d.Did)" +
                    ",(select rest from Dprice where Did=d.Did),(select currency from Dprice where Did=d.Did) from Doctor d order by d.Name", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //did,name,add,city,category,ll,cont,email,first,rest,currency
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string getAllDocs()
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select d.Did,d.Name,d.Address,d.City,d.Cate,d.Latlng,d.Cont,d.Email,(select first from Dprice where Did=d.Did)" +
                    ",(select rest from Dprice where Did=d.Did),(select currency from Dprice where Did=d.Did) from Doctor d order by d.Name", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //did,name,add,city,category,ll,cont,email,first,rest,currency
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string AddDoc(string Name, string Address, string city, string Cate, string Latlng, string Cont, string email, string first, string second)
        {
            string ans = "";
            string id = getId("D");
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Doctor where Email='" + email + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("insert into Doctor values('" + id + "','" + Name + "','" + Address + "','" + city + "','" + Cate + "','" + Latlng + "','" + Cont + "','" + email + "','" + Cont + "')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    cmd = new SqlCommand("insert into Dprice values('" + id + "','" + first + "','" + second + "','R')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string UpdateDoc(string did, string Name, string Address, string city, string Cate, string Latlng, string Cont, string email, string first, string second)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Doctor where Email='" + email + "' AND Did<>'" + did + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("update Doctor set Name='" + Name + "',Address='" + Address + "',City='" + city + "',Cate='" + Cate + "',Latlng='" + Latlng + "',Cont='" + Cont + "',Email='"
                    + email + "' where Did='" + did + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    cmd = new SqlCommand("update Dprice set first='" + first + "',rest='" + second + "' where Did='" + did + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string DelDoc(string did)
        {
            string ans = "";
            try
            {
                cmd = new SqlCommand("delete from Doctor where Did='" + did + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("delete from Appointment where Did='" + did + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("delete from Chatnames where did='" + did + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("delete from Chats where SenderId='" + did + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("delete from Chats where RecId='" + did + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("delete from Dprice where Did='" + did + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                ans = jm.Singlevalue("true");
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string getDiseaselist()
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Id,DName,Sym,Type from Dise order by DName", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Id,name,sym,type
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string AddDisease(string Name, string sym, string type)
        {
            string ans = "";
            string id = getId("Di");
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Dise where DName='" + Name + "' AND type='" + type + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("insert into Dise values('" + id + "','" + Name + "','" + sym + "','" + type + "','0')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string UpdateDisease(string id, string Name, string sym, string type)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Dise where DName='" + Name + "' AND type='" + type + "' AND Id <> '" + id + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("update Dise set DName='" + Name + "',Sym='" + sym + "',Type='" + type + "' where Id='" + id + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string DelDisease(string id)
        {
            string ans = "";
            try
            {
                cmd = new SqlCommand("delete from Dise where Id='" + id + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                ans = jm.Singlevalue("true");
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string getPatients()
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Pid,Pic,Name,Gender,DOB,Address,City,State,Cont,Email from Patient order by Name", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Pid,Pic,Name,Gender,DOB,Address,City,State,Cont,Email
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string getFeedback()
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select (select Name from Patient where Pid=f.Uid),(select Name from Doctor where Did=f.did),f.src,f.feed,f.fdate,f.ftime from Feedback f order by f.id DESC", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //name,dname,src,feed,date,time
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }


        //********************************
        //Patient
        //*******************************

        public string Register(string pic, string name, string gender, string dob, string address, string city, string state, string cont, string email, string pass)
        {
            string ans = "";
            string id = getId("P");
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Patient where Email='" + email + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("insert into Patient values('" + id + "','" + pic + "','" + name + "','" + gender + "','" + dob + "','" + address + "','" + city + "','"
                        + state + "','" + cont + "','" + email + "','" + pass + "')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string PLogin(string email, string pass)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Pid from Patient where Email='" + email + "' AND Pass='" + pass + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int count = ds.Tables[0].Rows.Count;
                if (count > 0)
                {
                    //Pid
                    ans = jm.Maker(ds);
                }
                else
                {
                    //false
                    ans = jm.Singlevalue("false");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;

        }

        public string PgetProfile(string pid)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Pid,Pic,Name,Gender,DOB,Address,City,State,Cont,Email from Patient where Pid='" + pid + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Pid,pic,Name,Gender,DOB,Address,City,State,Cont,Email 
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string PUpdateProfile(string pid, string pic, string name, string gender, string dob, string address, string city, string state, string cont, string email)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Patient where Email='" + email + "' AND pid<>'" + pid + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("update Patient set Pic='" + pic + "',Name='" + name + "',Gender='" + gender + "',DOB='" + dob + "',Address='" + address + "',City='" + city + "',State='"
                        + state + "',Cont='" + cont + "',Email='" + email + "' where Pid='" + pid + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    //true
                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string PChangePass(string pid, string oldpass, string newpass)
        {
            string ans = "";

            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Pass from Patient where pid='" + pid + "' AND Pass='" + oldpass + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    cmd = new SqlCommand("update Patient set Pass='" + newpass + "' where Pid='" + pid + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    //true
                    ans = jm.Singlevalue("true");
                }
                else
                {
                    //false
                    ans = jm.Singlevalue("false");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        //getDoctorslist

        //getDoctorslist

        //src-current/previous/pending/others
        public string PgetAppointment(string pid, string src, string date)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = null;
                if (src == "current")
                {
                    da = new SqlDataAdapter("select a.Aid,a.Did,(select Name from Doctor where Did=a.Did),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Pid='" + pid
                        + "' AND a.status='Confirmed' AND a.adate>='" + date + "' order by a.adate,a.atime", con);
                }
                else if (src == "previous")
                {
                    da = new SqlDataAdapter("select a.Aid,a.Did,(select Name from Doctor where Did=a.Did),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Pid='" + pid
                        + "' AND a.status='Confirmed' AND a.adate<'" + date + "' order by a.adate DESC,a.atime DESC", con);
                }
                else if (src == "pending")
                {
                    da = new SqlDataAdapter("select a.Aid,a.Did,(select Name from Doctor where Did=a.Did),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Pid='" + pid
                        + "' AND a.status='Pending' order by a.adate,a.atime", con);
                }
                else
                {
                    da = new SqlDataAdapter("select a.Aid,a.Did,(select Name from Doctor where Did=a.Did),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Pid='" + pid
                        + "' AND a.status<>'Pending' AND a.status<>'Confirmed' order by a.adate,a.atime", con);
                }

                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Aid,did,docname,note,date,time,status
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string PgetPriceInfo(string did, string pid)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Appointment where Did='" + did + "' AND Pid='" + pid + "' AND status='Confirmed'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    ans = jm.Singlevalue("first");
                }
                else
                {
                    ans = jm.Singlevalue("rest");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string PaddAppointment(string did, string pid, string note, string adate, string atime, string price, string date, string time)
        {
            string ans = "";
            string id = getId("A");
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Appointment where Did='" + did + "' AND Pid='" + pid
                    + "' AND adate='" + adate + "' AND atime='" + atime + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("insert into Appointment values('" + id + "','" + did + "','" + pid + "','" + note + "','" + price + "','" + adate + "','" + atime + "','Pending')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    AddTransaction(id, pid, did, price, "Appointment Booked", date, time);

                    AddtoNotification(did, pid, "New Appointment", "Appointment Booked by", date, time, "pat");

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string PcancelAppointment(string aid, string did, string pid, string price, string date, string time)
        {
            string ans = "";
            int count = Int32.Parse(getcancelCount(pid));
            try
            {
                if (count > 10)
                {
                    ans = jm.Singlevalue("false");
                }
                else
                {
                    cmd = new SqlCommand("update Appointment set status='Cancelled' where aid='" + aid + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    AddTransaction(aid, did, pid, price, "Appointment Cancelled", date, time);

                    AddtoNotification(did, pid, "Appointment Cancelled", "Appointment Cancelled by", date, time, "pat");

                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string PgetChats_Names(string pid)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select did,DName from Chatnames where pid='"+pid+"'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //did,Dname
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string PgetChatsbyId(string pid, string did)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select SenderId,RecId,Message,cdate,ctime from Chats where (SenderId='" + pid
                    + "' AND RecId='" + did + "') OR (SenderId='" + did + "' AND RecId='" + pid + "') order by cid", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //SenderId,RecId,Message,cdate,ctime
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        //src - pat/doc
        public string AddChat(string sender, string rec, string message, string date, string time, string src)
        {
            string ans = "";
            try
            {
                cmd = new SqlCommand("insert into Chats values('" + sender + "','" + rec + "','" + message + "','','" + date + "','" + time + "')", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                if (src == "pat")
                {
                    addToChat(sender, rec);
                    AddtoNotification(rec, sender, "Message from", message, date, time, "pat");
                }
                else
                {
                    addToChat(rec, sender);
                    AddtoNotification(rec, sender, "Message from", message, date, time, "doc");
                }



                ans = jm.Singlevalue("true");
            }
            catch (Exception e)
            {
                ans = e.Message;
            }
            return ans;
        }

        public string PgetFeedback(string pid)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select (select Name from Doctor where Did=f.did),f.src,f.feed,f.fdate,f.ftime from Feedback f where f.Uid='" + pid + "' order by f.id DESC", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //name,src,feed,date,time
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string PAddFeedback(string uid, string did, string src, string feed, string date, string time)
        {
            string ans = "";
            try
            {
                cmd = new SqlCommand("insert into Feedback values('" + uid + "','" + did + "','" + src + "','" + feed + "','" + date + "','" + time + "')", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                if (src != "Admin")
                {
                    AddtoNotification(did, "no", "New Feedback", feed, date, time, "pat");
                }

                ans = jm.Singlevalue("true");
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        //debit/credit
        public string PgetTransactions(string pid, string src)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = null;
                if (src == "debit")
                {
                    da = new SqlDataAdapter("select t.Aid,(select Name from Doctor where Did=t.RecieverId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.SenderId='" +
                        pid + "' order by t.Tid DESC", con);
                }
                else
                {
                    da = new SqlDataAdapter("select t.Aid,(select Name from Doctor where Did=t.SenderId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.RecieverId='" +
                        pid + "' order by t.Tid DESC", con);
                }
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Aid,Sender/Reciever,price,status,date,time
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public void getSym()
        {
            SqlDataAdapter da = new SqlDataAdapter("Select Sym from Dise where Flag = '1'", con);
            DataSet ds = new DataSet();
            da.Fill(ds);
            int row = ds.Tables[0].Rows.Count;
            string sys = "";
            SqlCommand cmd;
            int i = 0;
        Start:
            while (i < row)
            {
                sys = ds.Tables[0].Rows[i][0].ToString();
                string[] values = sys.Split(',');

                int val = values.Length;
                string d1 = "";
                for (int i2 = 0; i2 < val; i2++)
                {
                    cmd = new SqlCommand("Select * from Final where Sym Like '%" + values[i2].ToLower() + "%'", con);
                    con.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    d1 = values[i2];
                    if (dr.HasRows)
                    {
                        con.Close();

                    }
                    else
                    {
                        con.Close();
                        cmd = new SqlCommand("Insert into Keyword values ('" + values[i2] + "')", con);
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                        i++;
                        goto Start;
                    }
                }
                i++;
            }
        }

        public string sysone(string sys)
        {
            string op = "false";
            try
            {
                SqlCommand cmd = new SqlCommand("Delete From Final", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("Delete From Keyword", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("Update Dise Set Flag = '0'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();


                SqlDataAdapter da = new SqlDataAdapter("Select DName from Dise Where Sym LIKE '%" + sys + "%'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int row = ds.Tables[0].Rows.Count;

                if (row != 0)
                {
                    cmd = new SqlCommand("Update Dise Set Flag='1' Where Sym LIKE '%" + sys + "%'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    cmd = new SqlCommand("Insert into Final Values ('" + sys.ToLower() + "')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    getSym();

                    da = new SqlDataAdapter("Select distinct Sym from Keyword", con);
                    ds = new DataSet();
                    da.Fill(ds);
                    row = ds.Tables[0].Rows.Count;

                    //symptoms
                    op = jm.Maker(ds);
                }
                else
                {
                    //no
                    op = jm.Singlevalue("no");
                }
            }
            catch (Exception ep)
            {
                op = jm.Error(ep.Message);
            }
            return op;
        }

        public string systwo(string sys)
        {
            string op = "false";
            try
            {
                SqlCommand cmd = new SqlCommand("Delete From Keyword", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                cmd = new SqlCommand("Update Dise Set Flag = '0'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                SqlDataAdapter da = new SqlDataAdapter("Select DName from Dise Where Sym LIKE '%" + sys + "%'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int row = ds.Tables[0].Rows.Count;

                if (row != 0)
                {
                    cmd = new SqlCommand("Insert into Final Values ('" + sys.ToLower() + "')", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    SqlDataAdapter da1 = new SqlDataAdapter("Select Sym from Final", con);
                    DataSet ds1 = new DataSet();
                    da1.Fill(ds1);
                    int row1 = ds1.Tables[0].Rows.Count;


                    string s = "Update Dise Set Flag='1' Where ", s1 = "";
                    for (int i = 0; i < row1; i++)
                    {
                        s1 += " Sym LIKE '%" + ds1.Tables[0].Rows[i][0].ToString() + "%' And ";
                    }
                    s1 = s1.Remove(s1.Length - 4);

                    string com = s + s1;
                    cmd = new SqlCommand(com, con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    getSym();

                    da = new SqlDataAdapter("Select Sym from Keyword", con);
                    ds = new DataSet();
                    da.Fill(ds);
                    row = ds.Tables[0].Rows.Count;
                    if (row > 0)
                    {
                        //symtoms
                        op = jm.Maker(ds);
                    }
                    else
                    {
                        op = jm.Singlevalue("no");
                    }
                }
                else
                {
                    //no - call final
                    op = jm.Singlevalue("no");
                }
            }
            catch (Exception ep)
            {
                //no - call final
                op = jm.Singlevalue("no");
            }
            return op;
        }

        public string final1(string ID, string Date)
        {
            string op = "false";
            string fd = "", ft = "", fdo = "";
            try
            {
                SqlDataAdapter da1 = new SqlDataAdapter("Select Sym from Final", con);
                DataSet ds1 = new DataSet();
                da1.Fill(ds1);
                int row1 = ds1.Tables[0].Rows.Count;

                string s = "Select DName,Type From Dise Where ", s1 = "";
                for (int i = 0; i < row1; i++)
                {
                    s1 += " Sym LIKE '%" + ds1.Tables[0].Rows[i][0].ToString() + "%' And ";
                }
                s1 = s1.Remove(s1.Length - 4);

                string com = s + s1;
                da1 = new SqlDataAdapter(com, con);
                ds1 = new DataSet();
                da1.Fill(ds1);
                row1 = ds1.Tables[0].Rows.Count;

                DataTable dt = new DataTable();
                dt.Columns.Add("disease");
                dt.Columns.Add("type");
                dt.Columns.Add("doc");

                if (row1 != 0)
                {
                    string sy = "";
                    op = "";
                    for (int i = 0; i < row1; i++)
                    {
                        sy += ds1.Tables[0].Rows[i][0].ToString();
                        op += ds1.Tables[0].Rows[i][0].ToString() + "$";
                    }
                    op = op.Remove(op.Length - 1);
                    fd = op.Replace('$', '*');
                    op += "*";

                    string fin = ds1.Tables[0].Rows[0][1].ToString();
                    ft = fin;
                    op += ds1.Tables[0].Rows[0][1].ToString() + "*";

                    SqlDataAdapter da = new SqlDataAdapter("Select Sym from Final", con);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    int cou = ds.Tables[0].Rows.Count;
                    string sys = "";

                    for (int i = 0; i < cou; i++)
                    {
                        sys += ds.Tables[0].Rows[i][0].ToString() + ",";
                    }
                    sys.Remove(sys.Length - 1);

                    SqlCommand cmd = new SqlCommand("Insert into history(UId,Sym,Disease,type,Date) Values ('" + ID + "','" + sys + "','" + sy + "','" + fin + "','" + DateTime.Now.ToString() + "') ", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    DataRow drow = dt.NewRow();
                    drow["disease"] = fd;
                    drow["type"] = ft;

                    da = new SqlDataAdapter("Select d.Did,d.Name,d.Address,d.City,d.Cate,d.Latlng,d.Cont,d.Email,(select first from Dprice where Did=d.Did)" +
                    ",(select rest from Dprice where Did=d.Did),(select currency from Dprice where Did=d.Did) from Doctor d where d.Cate Like '%" + ds1.Tables[0].Rows[0][1].ToString() + "%'", con);
                    ds = new DataSet();
                    da.Fill(ds);
                    int row = ds.Tables[0].Rows.Count;
                    if (row != 0)
                    {
                        string ans = "";
                        for(int i=0;i<row;i++)
                        {
                            ans += ds.Tables[0].Rows[i][0].ToString()+"*";
                            ans += ds.Tables[0].Rows[i][1].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][2].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][3].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][4].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][5].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][6].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][7].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][8].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][9].ToString() + "*";
                            ans += ds.Tables[0].Rows[i][10].ToString() + "#";
                        }
                        drow["doc"] = ans;
                    }
                    else
                    {
                        drow["doc"] = "no";
                    }

                    dt.Rows.Add(drow);

                    //data0 - diseases *
                    //data1 - type
                    //data2 - status - ok - Did,Name,Address,City,Cate,Latlng,Cont,Email
                    //      - status - no
                    op = jm.Maker1(dt);

                }
                else
                {
                    //no
                    op = jm.Singlevalue("no");
                }
            }
            catch (Exception ep)
            {
                op = jm.Error(ep.Message);
            }
            return op;
        }


        //********************************
        //Doctor
        //*******************************

        public string DLogin(string email, string pass)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Did from Doctor where Email='" + email + "' AND Pass='" + pass + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                int count = ds.Tables[0].Rows.Count;
                if (count > 0)
                {
                    //Pid
                    ans = jm.Maker(ds);
                }
                else
                {
                    //false
                    ans = jm.Singlevalue("false");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;

        }

        public string DgetProfile(string did)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("Select d.Did,d.Name,d.Address,d.City,d.Cate,d.Latlng,d.Cont,d.Email,(select first from Dprice where Did=d.Did)" +
                    ",(select rest from Dprice where Did=d.Did),(select currency from Dprice where Did=d.Did) from Doctor d where d.Did='" + did + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Did,Name,Address,City,Cate,Latlng,Cont,Email,first,rest,currency
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string DUpdateProfile(string did, string name, string address, string city, string cate, string latlng, string cont, string email,string first,string rest)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select * from Doctor where Email='" + email + "' AND Did<>'" + did + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    //already
                    ans = jm.Singlevalue("already");
                }
                else
                {
                    cmd = new SqlCommand("update Doctor set Name='" + name + "',Address='" + address + "',City='" + city + "',Cate='"
                        + cate + "',Latlng='" + latlng + "',Cont='" + cont + "',Email='" + email + "' where Did='" + did + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    cmd = new SqlCommand("update Dprice set first='" + first + "',rest='" + rest + "' where Did='" + did + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    //true
                    ans = jm.Singlevalue("true");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        public string DChangePass(string did, string oldpass, string newpass)
        {
            string ans = "";

            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select Pass from Doctor where Did='" + did + "' AND Pass='" + oldpass + "'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    cmd = new SqlCommand("update Doctor set Pass='" + newpass + "' where Did='" + did + "'", con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();

                    //true
                    ans = jm.Singlevalue("true");
                }
                else
                {
                    //false
                    ans = jm.Singlevalue("false");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        //getPatients

        //getDiseaselist

        public string DgetChats_Names(string did)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select pid,PName from Chatnames where did='"+did+"'", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //pid,pname
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string DgetChatsbyId(string pid, string did)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select SenderId,RecId,Message,cdate,ctime from Chats where (SenderId='" + did
                    + "' AND RecId='" + pid + "') OR (SenderId='" + pid + "' AND RecId='" + did + "')  order by cid", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //SenderId,RecId,Message,cdate,ctime
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        //AddChat

        //src-current/previous/pending/others
        public string DgetAppointment(string did, string src, string date)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = null;
                if (src == "current")
                {
                    da = new SqlDataAdapter("select a.Aid,a.Pid,(select Name from Patient where Pid=a.Pid),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Did='" + did
                        + "' AND a.status='Confirmed' AND a.adate>='" + date + "' order by a.adate,a.atime", con);
                }
                else if (src == "previous")
                {
                    da = new SqlDataAdapter("select a.Aid,a.Pid,(select Name from Patient where Pid=a.Pid),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Did='" + did
                        + "' AND a.status='Confirmed' AND a.adate<'" + date + "' order by a.adate DESC,a.atime DESC", con);
                }
                else if (src == "pending")
                {
                    da = new SqlDataAdapter("select a.Aid,a.Pid,(select Name from Patient where Pid=a.Pid),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Did='" + did
                        + "' AND a.status='Pending' order by a.adate,a.atime", con);
                }
                else
                {
                    da = new SqlDataAdapter("select a.Aid,a.Pid,(select Name from Patient where Pid=a.Pid),a.note,a.price,a.adate,a.atime,a.status from Appointment a where a.Did='" + did
                        + "' AND a.status<>'Pending' AND a.status<>'Confirmed' order by a.adate,a.atime", con);
                }

                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Aid,did,docname,note,date,time,status
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        //status - Confirmed/Rejected
        public string DChangeStatus(string aid, string did, string pid, string price, string date, string time, string status)
        {
            string ans = "";
            try
            {
                cmd = new SqlCommand("update Appointment set status='" + status + "' where aid='" + aid + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                if (status == "Rejected")
                {
                    AddTransaction(aid, did, pid, price, "Appointment Rejected", date, time);
                }

                AddtoNotification(pid, did, "Appointment " + status, "Appointment is " + status + " by", date, time, "doc");

                ans = jm.Singlevalue("true");
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }

            return ans;
        }

        //debit/credit
        public string DgetTransactions(string did, string src)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = null;
                if (src == "debit")
                {
                    da = new SqlDataAdapter("select t.Aid,(select Name from Patient where Pid=t.RecieverId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.SenderId='" +
                        did + "' order by t.Tid DESC", con);
                }
                else
                {
                    da = new SqlDataAdapter("select t.Aid,(select Name from Patient where Pid=t.SenderId),t.price,t.status,t.tdate,t.ttime from ATransaction t where t.RecieverId='" +
                        did + "' order by t.Tid DESC", con);
                }
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //Aid,Sender/Reciever,price,status,date,time
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

        public string DgetFeedback(string did)
        {
            string ans = "";
            try
            {
                SqlDataAdapter da = new SqlDataAdapter("select (select Name from Patient where Pid=f.Uid),f.feed,f.fdate,f.ftime from Feedback f where f.did='"+did+"' order by f.id DESC", con);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (ds.Tables[0].Rows.Count > 0)
                {
                    //name,dname,src,feed,date,time
                    ans = jm.Maker(ds);
                }
                else
                {
                    //no
                    ans = jm.Singlevalue("no");
                }
            }
            catch (Exception e)
            {
                ans = jm.Error(e.Message);
            }
            return ans;
        }

    }
}

