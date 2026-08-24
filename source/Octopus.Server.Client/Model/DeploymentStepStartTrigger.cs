using System;

namespace Octopus.Client.Model
{
    public enum DeploymentStepStartTrigger
    {
        StartAfterPrevious = 0,
        StartWithPrevious = 1,
        Variable = 2
    }
}
