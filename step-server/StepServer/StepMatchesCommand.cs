using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Newtonsoft.Json.Linq;

namespace StepServer
{
  public class StepMatchesCommand: IStepCommand
  {
    private Assembly _stepsAssembly;
    private string _payload;

    private string MatchValue
    {
      get
      {
        var parsedPayload = JObject.Parse(_payload);
        return parsedPayload["name_to_match"].Value<string>();
      }
    }

    public StepMatchesCommand(Assembly stepsAssembly, string payload)
    {
      _stepsAssembly = stepsAssembly;
      _payload = payload;
    }

    public IStepResponse Execute()
    {
      IStepResponse response = null;

      // For each class in the assembly
      foreach (var type in _stepsAssembly.GetTypes())
      {
        if (type.IsPublic && type.IsClass && !type.IsAbstract)
        {
          foreach (var member in type.GetMembers())
          {
            foreach (var attribute in member.CustomAttributes)
            {
              if (attribute.AttributeType.Name == "WhenAttribute")
              {
                var regexPattern = attribute.ConstructorArguments[0];
                var regex = new Regex(regexPattern.Value.ToString());
                var match = regex.Match(MatchValue);

                if (match.Success)
                {
                  var args = new List<string>();
                  foreach (var group in match.Groups)
                  {
                    if (group != match)
                    {
                      args.Add(group.ToString());
                    }
                  }

                  response = new StepMatchesResponse(
                    match.Success, "1", args.ToArray()
                  );
                }
              }
            }
          }
        }
      }

      if (response == null)
      {
        response = new StepMatchesResponse(false);
      }

      return response;
    }
  }
}