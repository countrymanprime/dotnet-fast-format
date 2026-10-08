class C
{
    void M()
    {
        try { Run(); } catch { }
        try
        {
            Run();
        }
        catch (IOException)
        {
            Handle();
        }
        catch (Exception ex) when (ex.HResult == 5)
        {
            Handle(ex);
        }
        catch (Exception ex) when (firstConditionWithALongName && secondConditionWithALongName && thirdConditionWithALongName)
        {
        }
        finally
        {
            Cleanup();
        }
        try { Run(); } finally { Cleanup(); }
        try
        {
            Run();
        } // after try
        catch
        {
            throw;
        }
        try
        {
            Run();
        }
        // before catch
        catch
        {
        }
        try
        {
            Run();
        }
        catch { } // after catch
        int after = 1;
    }
}
