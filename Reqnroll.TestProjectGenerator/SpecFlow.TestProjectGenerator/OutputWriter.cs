﻿namespace TechTalk.SpecFlow.TestProjectGenerator
{
    public interface IOutputWriter
    {
        void WriteLine(string message);
        void WriteLine(string format, params object[] args);
    }
}
