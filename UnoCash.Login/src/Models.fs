module UnoCash.Login.Models

type Model =
    {
        ApiBaseUrl : string
    }
    
let emptyModel = 
    {
        ApiBaseUrl = "http://localhost:7071"
    }