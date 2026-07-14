using System.Data;

namespace JSONWebAPI
{
    public interface IServiceAPI
    {
        string getNotification(string uid);

        string ALogin(string usern, string pass);
        string getDoctors();
        string getAllDocs();
        string AddDoc(string Name, string Address, string city, string Cate, string Latlng, string Cont, string email, string first, string second);
        string UpdateDoc(string did, string Name, string Address, string city, string Cate, string Latlng, string Cont, string email, string first, string second);
        string DelDoc(string did);
        string getDiseaselist();
        string AddDisease(string Name, string sym, string type);
        string UpdateDisease(string id, string Name, string sym, string type);
        string DelDisease(string id);
        string getPatients();
        string getFeedback();

        string Register(string pic, string name, string gender, string dob, string address, string city, string state, string cont, string email, string pass);
        string PLogin(string email, string pass);
        string PgetProfile(string pid);
        string PUpdateProfile(string pid, string pic, string name, string gender, string dob, string address, string city, string state, string cont, string email);
        string PChangePass(string pid, string oldpass, string newpass);
        string PgetAppointment(string pid, string src, string date);
        string PgetPriceInfo(string did, string pid);
        string PaddAppointment(string did, string pid, string note, string adate, string atime, string price, string date, string time);
        string PcancelAppointment(string aid, string did, string pid, string price, string date, string time);
        string PgetChats_Names(string pid);
        string PgetChatsbyId(string pid, string did);
        string AddChat(string sender, string rec, string message, string date, string time, string src);
        string PgetFeedback(string pid);
        string PAddFeedback(string uid, string did, string src, string feed, string date, string time);
        string PgetTransactions(string pid, string src);
        void getSym();
        string sysone(string sys);
        string systwo(string sys);
        string final1(string ID, string Date);

        string DLogin(string email, string pass);
        string DgetProfile(string did);
        string DUpdateProfile(string did, string name, string address, string city, string cate, string latlng, string cont, string email, string first, string rest);
        string DChangePass(string did, string oldpass, string newpass);
        string DgetChats_Names(string did);
        string DgetChatsbyId(string pid, string did);
        string DgetAppointment(string did, string src, string date);
        string DChangeStatus(string aid, string did, string pid, string price, string date, string time, string status);
        string DgetTransactions(string did, string src);
        string DgetFeedback(string did);
    }
}