module Pulumi.FSharp.Azure

type Region =
    | WestEurope
    
let regionName =
    function
    | WestEurope -> "West Europe"