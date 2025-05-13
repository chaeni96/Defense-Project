namespace Kylin.LWDI
{
    public class BaseModel : IDependencyObject, IInjectable
    {
        protected BaseModel()
        {
            Inject();
        }
        
        public void Inject()
        {
            DependencyInjector.Inject(this);
        }
    }
}