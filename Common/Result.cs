
    public class Result {
    public Result () {
        this.StatusCode = "000";
        this.Description = "Successfull Transaction";
    }

     public Result (string code, string message) {
        this.StatusCode = code;
        this.Description = message;
    }
    public string StatusCode {get;set;}
    public string Description {get;set;}
}


