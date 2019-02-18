
    public class Result {
    public Result () {
        this.StatusCode = ResultCodes.Success;
        this.Description = "Successfull Transaction";
    }

     public Result (string code, string message) {
        this.StatusCode = code;
        this.Description = message;
    }
    public string StatusCode {get;set;}
    public string Description {get;set;}
    public object Data {get;set;}
}


