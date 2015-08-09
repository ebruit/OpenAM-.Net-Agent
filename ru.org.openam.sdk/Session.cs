﻿using System;
using System.Net;

namespace ru.org.openam.sdk
{
	public class Session
    {
		private readonly Agent agent;

		private static readonly Cache _cache = new Cache();

		internal Cache PolicyCache {get; private set;}=new Cache();

        public String sessionId;

        public session.Response token;
		
        public Session(string sessionId)
        {
            token = Get(new session.Request(sessionId));
            this.sessionId = sessionId;
        }

		public Session(auth.Response authResponse)
		{
			session.Request request = new session.Request(authResponse.ssoToken);
			request.cookieContainer = authResponse.cookieContainer;
			//remove AMAuthCookie after auth
			CookieCollection cc = request.cookieContainer.GetCookies (request.getUrl());
			foreach (Cookie co in cc)
				if (co.Name.Equals ("AMAuthCookie"))
					co.Expired = true;
			token = Get(request);
			this.sessionId = token.sid;
		}

        private Session(Agent agent, string authCookie)
            : this(authCookie)
        {
            this.agent = agent;
        }

        public static Session getSession(Agent agent, string authCookie)
        {
			if (authCookie == null)
			{
				return null;
			}

			var minsStr = agent.GetSingle("com.sun.identity.agents.config.policy.cache.polling.interval");
			int mins;
			if (!int.TryParse(minsStr, out mins))
			{
				mins = 1;
			}

			var userSession = _cache.GetOrDefault
			(
				"am_" + authCookie,
				() => new Session(agent,authCookie),
				mins
				// всегда приходит 0
				//, r =>
				//{
				//	if (r != null && r.token != null)
				//	{
				//		return r.token.maxcaching;
				//	}

				//	return 3;
				//}
			);
            return userSession;
        }

        public void Validate() 
        {
            token = Get(new session.Request(this));
        }

        public bool isValid()
        {
            try
            {
                Validate();
                return true;
            }
            catch (session.SessionException)
            {
                return false;
            }
        }

        public naming.Response GetNaming() //for personal session naming (need agent only)
        {
            return agent==null?Bootstrap.GetNaming():agent.GetNaming();
        }

        public session.Response Get(session.Request request)
        {
			return (session.Response)request.getResponse();
        }

        override public String ToString()
        {
            return "Session: " + sessionId;
        }

        public String GetProperty(String key, String value)
        {
            String res = GetProperty(key);
            return res ?? value;
        }
        public String GetProperty(String key)
        {
            Validate();
            return token.property[key];
        }
    }
}
